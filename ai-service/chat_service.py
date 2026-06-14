# chat_service.py
from __future__ import annotations

from contextlib import asynccontextmanager
from fastapi import FastAPI, Body, File, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from openai import OpenAI, OpenAIError
from dotenv import load_dotenv
import base64, io, mimetypes, os, json, numpy as np
from datetime import date
from typing import Literal

load_dotenv()

# ── STARTUP: pre-build index from products.json ──────────────
# This runs before the first request, so the chatbot is ready
# immediately. .NET's POST /index can still refresh it later.
@asynccontextmanager
async def lifespan(app: FastAPI):
    _build_index_from_file()
    yield

app = FastAPI(lifespan=lifespan)

# Lock CORS to configured origin — replace with your real domain before going live.
# While Python is only called by .NET (not the browser), this is defence-in-depth.
_allowed_origin = os.getenv('ALLOWED_ORIGIN', 'http://localhost:4200')
app.add_middleware(
    CORSMiddleware,
    allow_origins=[_allowed_origin],
    allow_methods=['POST'],
    allow_headers=['Content-Type'],
)
client = OpenAI(api_key=os.getenv('OPENAI_API_KEY'))

# ── EMBEDDING INDEX ──────────────────────────────────────────
# Holds the product list + their embeddings in memory.
# Pre-built from products.json at startup; refreshed when .NET calls POST /index.
_MAX_INDEX_SIZE = 50   # raise both here and .Take(50) in EmbeddingIndexService.cs together
_product_index: list[dict] = []   # [{product, embedding}, ...]

_PRODUCTS_FILE = os.path.join(os.path.dirname(__file__), 'products.json')

def _embed(text: str) -> np.ndarray:
    """Get a single embedding vector from OpenAI."""
    res = client.embeddings.create(
        model='text-embedding-3-small',
        input=text
    )
    return np.array(res.data[0].embedding, dtype=np.float32)

def _cosine(a: np.ndarray, b: np.ndarray) -> float:
    """Cosine similarity between two vectors (0.0 – 1.0)."""
    return float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-9))

def _build_index_from_file():
    """Load products.json and build the embedding index at startup."""
    global _product_index
    if not os.path.exists(_PRODUCTS_FILE):
        print('[index] products.json not found — skipping pre-build')
        return
    with open(_PRODUCTS_FILE, encoding='utf-8') as f:
        products = json.load(f)
    _product_index = []
    for p in products[:_MAX_INDEX_SIZE]:
        # Normalize products.json { id, text } into the same shape as live DB products
        # Text format: "Name. Price: $X. Description..."
        if 'name' not in p and 'text' in p:
            raw = p['text']
            if '. Price: $' in raw:
                name_part, rest = raw.split('. Price: $', 1)
                price_str, *desc_parts = rest.split('. ', 1)
                try:
                    price = float(price_str.replace(',', ''))
                except ValueError:
                    price = 0.0
                description = desc_parts[0] if desc_parts else ''
            else:
                name_part = raw.split('.')[0]
                price = 0.0
                description = raw
            normalized = {
                'name':        name_part.strip(),
                'price':       price,
                'description': description.strip(),
                'inStock':     True,
            }
        else:
            normalized = p  # already in DB format
        text = f"{normalized.get('name', '')} — {normalized.get('description', '')}"
        _product_index.append({
            'product':   normalized,
            'embedding': _embed(text)
        })
    print(f'[index] Pre-built from products.json: {len(_product_index)} products')

def _semantic_search(query: str, top_k: int = 8) -> list[dict]:
    """Return the top_k most relevant products for the query."""
    if not _product_index:
        return []
    q_vec = _embed(query)
    scored = [
        (item['product'], _cosine(q_vec, item['embedding']))
        for item in _product_index
    ]
    scored.sort(key=lambda x: x[1], reverse=True)
    return [p for p, score in scored[:top_k] if score > 0.25]

# ── POLICIES ─────────────────────────────────────────────────
_POLICIES_FILE = os.path.join(os.path.dirname(__file__), 'policies.json')
_policies: dict = {}
if os.path.exists(_POLICIES_FILE):
    with open(_POLICIES_FILE, encoding='utf-8') as _f:
        _policies = json.load(_f)

def _policies_summary() -> str:
    if not _policies:
        return ''
    c = _policies.get('cancellation', {})
    b = _policies.get('booking', {})
    ch = _policies.get('changes', {})
    inc = _policies.get('inclusions', {})
    age = _policies.get('age_policy', {})
    tiers = '; '.join(
        f"{t['days_before_departure']}+ days = {t['refund_percent']}% refund"
        for t in c.get('tiers', [])
    )
    always = ', '.join(inc.get('always_included', []))
    never  = ', '.join(inc.get('never_included', []))
    return f"""
Store policies you must follow when answering questions:
- Deposit: {b.get('deposit_percent')}% required to confirm. Balance due 30 days before departure.
- Minimum booking lead time: {b.get('minimum_advance_days')} days before departure.
- Cancellation refunds: {tiers}.
- Date changes: allowed up to 14 days before departure for ${ch.get('date_change_fee_usd')} per person.
- Always included: {always}.
- Never included: {never}.
- Children under 2 travel free; ages 2–11 get a 15% discount; under 18 must travel with an adult.
- Accepted payments: {', '.join(_policies.get('payment', {}).get('accepted_methods', []))}.
- Loyalty programme: {_policies.get('loyalty', {}).get('points_per_dollar')} point per $1 spent; 500 points = $10 off next booking.
If a customer asks about any of these topics, answer clearly and factually using the above. Do not invent or modify policy details.
"""

# ── SYSTEM PROMPT ────────────────────────────────────────────
# This is the agent's persona. Edit this — not the code below.
SYSTEM_PROMPT = f"""
You are Luna, a personal travel advisor at {os.getenv('STORE_NAME')}.
{os.getenv('STORE_DESCRIPTION')}
Today's date is {date.today().strftime('%B %d, %Y')}.

Your tone is warm and friendly — like a well-travelled friend, not a salesperson.

Conversation flow — follow this exact order, one question per reply:
Step 1: Ask what KIND of vacation (beach / city & culture / adventure & nature / ski & snow).
Step 2: Ask what MONTH or travel period.
Step 3: Ask what BUDGET per person.
Step 4: Recommend 1-2 packages from the catalog that best match. ONLY recommend packages that appear in the catalog list provided below. Do not invent packages.

Rules:
- STRICT 2-sentence maximum per reply. Never write more than 2 sentences.
- One question per reply. Never ask two things at once.
- Never repeat a question already answered in the conversation.
- Skip any step if the customer already gave that info.
- Once you have kind + month + budget, go straight to a recommendation — no more questions.
- Only recommend packages from the catalog list given to you. If none match, say so honestly.
- Never discuss politics, safety, visas, or competitors.

Output format for a recommendation (use exactly this, nothing more):
✈️ [Package name] — [one sentence why it fits].
Would you like to see it?
{_policies_summary()}
"""

# ── DATA MODELS ──────────────────────────────────────────────
class Message(BaseModel):
    role: str      # 'user' or 'assistant'
    content: str

class ChatRequest(BaseModel):
    message: str
    history: list[Message] = []
    products: list = []

class SearchRequest(BaseModel):
    query: str
    products: list = []   # optional: sent from .NET if index not built yet
    top_k: int = 5

# ── INDEX ENDPOINT ───────────────────────────────────────────
# .NET calls this to refresh the index with live DB data.
# Overrides the products.json index built at startup.

@app.post('/index')
async def build_index(products: list = Body(...)):
    global _product_index
    _product_index = []
    capped = products[:_MAX_INDEX_SIZE]   # never embed more than the cap
    for p in capped:
        text = f"{p.get('name', '')} — {p.get('description', '')}"
        _product_index.append({
            'product':   p,
            'embedding': _embed(text)
        })
    return {'indexed': len(_product_index), 'received': len(products), 'capped_at': _MAX_INDEX_SIZE}

# ── SEARCH ENDPOINT ──────────────────────────────────────────
# Standalone semantic search — useful for the Angular search bar.
# Uses pre-computed index (fast, no extra API cost per product).
# Falls back to per-product embedding if index not yet built.
@app.post('/search')
async def search(req: SearchRequest):
    # Fast path: use pre-computed index (no per-product API calls)
    if _product_index:
        q_vec = _embed(req.query)
        scored = [
            {**item['product'], 'score': round(_cosine(q_vec, item['embedding']), 3)}
            for item in _product_index
        ]
        scored.sort(key=lambda x: x['score'], reverse=True)
        return {'results': [r for r in scored[:req.top_k] if r['score'] > 0.25]}

    # Fallback: embed each product on the fly (small catalogs / index not built yet)
    if not req.products:
        return {'results': []}

    query_embedding = _embed(req.query)
    scored = []
    for p in req.products:
        product_text = f"{p.get('name', '')} {p.get('description', '')}"
        score = _cosine(query_embedding, _embed(product_text))
        scored.append({**p, 'score': round(score, 3)})

    scored.sort(key=lambda x: x['score'], reverse=True)
    return {'results': scored[:req.top_k]}

# ── CHAT ENDPOINT ────────────────────────────────────────────
_MAX_MESSAGE_CHARS = 1000   # ~250 words — enough for any reasonable query

@app.post('/chat')
async def chat(req: ChatRequest):
    # ── Input guards ────────────────────────────────────────
    msg = req.message.strip()
    if not msg:
        return {'reply': "I didn't catch that — could you tell me a bit about the kind of vacation you're dreaming of?"}
    if len(msg) > _MAX_MESSAGE_CHARS:
        msg = msg[:_MAX_MESSAGE_CHARS]   # truncate silently; still process the request

    # Use semantic search if we have an index; fall back to the
    # products list .NET sent (for backwards compatibility).
    if _product_index:
        relevant = _semantic_search(req.message, top_k=8)
    else:
        relevant = req.products   # fallback: full list from .NET

    if relevant:
        catalog_lines = []
        for p in relevant:
            # All products are now normalized to name/price/description/inStock
            name  = p.get('name', '')
            price = p.get('price', 'N/A')
            desc  = p.get('description', '')
            stock = 'available this month' if p.get('inStock') else 'not available this month'
            line  = f"- {name} (${price}) [{stock}]: {desc}"
            catalog_lines.append(line)
        catalog = '\n'.join(catalog_lines)
        full_prompt = (
            SYSTEM_PROMPT +
            f'\n\nMost relevant vacation packages for this message:\n{catalog}'
            '\n\nOnly recommend packages from this list. '
            'If a package is marked \'not available this month\', do not recommend it — '
            'suggest an available alternative instead.'
        )
    else:
        full_prompt = SYSTEM_PROMPT

    # Build conversation
    messages = [{'role': 'system', 'content': full_prompt}]
    for m in req.history:
        messages.append({'role': m.role, 'content': m.content})
    messages.append({'role': 'user', 'content': msg})

    response = client.chat.completions.create(
        model='gpt-4o',
        messages=messages,
        max_tokens=400,
        temperature=0.7
    )
    reply_text = response.choices[0].message.content

    # Detect if a product name from the catalog was mentioned in the reply
    suggested_search: str | None = None
    if relevant:
        reply_lower = reply_text.lower()
        for p in relevant:
            # All products are now normalized — always has 'name'
            name = p.get('name', '')
            if name and name.lower() in reply_lower:
                suggested_search = name
                break

    return {'reply': reply_text, 'suggestedSearch': suggested_search}


# ════════════════════════════════════════════════════════════════════
# DREAM VACATION GENERATOR
# Analyses a user's vacation profile from three input types:
#   1. Free text        → POST /dream-vacation/analyze/text
#   2. Audio recording  → POST /dream-vacation/analyze/audio  (Whisper)
#   3. Inspiration image→ POST /dream-vacation/analyze/image  (GPT-4o Vision)
# All three paths converge in _parse_vacation_profile().
# ════════════════════════════════════════════════════════════════════

_DV_MODEL        = 'gpt-4o'
_WHISPER_MODEL   = 'whisper-1'
_MAX_AUDIO_BYTES = 25 * 1024 * 1024   # Whisper hard limit
_MAX_IMAGE_BYTES = 20 * 1024 * 1024

_ALLOWED_AUDIO_MIME: frozenset[str] = frozenset({
    'audio/mpeg', 'audio/mp4', 'audio/wav', 'audio/webm',
    'audio/ogg', 'audio/flac', 'audio/x-m4a',
})
_ALLOWED_IMAGE_MIME: frozenset[str] = frozenset({
    'image/jpeg', 'image/png', 'image/gif', 'image/webp',
})

# ── Structured output types ──────────────────────────────────
BudgetLevel   = Literal['low', 'medium', 'high']
TravelTwinType = Literal[
    'Explorer', 'Luxury Traveler', 'Nature Escapist',
    'Urban Discoverer', 'Adrenaline Hunter',
]

class VacationAnalysis(BaseModel):
    detected_vibe: str = Field(..., description='Overall emotional vibe in 2-4 words.')
    requested_weather: str = Field(..., description='Preferred climate or season.')
    pace: str = Field(..., description='Exactly one of: leisurely, moderate, action-packed.')
    estimated_budget_level: BudgetLevel = Field(
        ..., description='low=budget/hostels, medium=3-star, high=luxury/5-star.'
    )

class VacationProfile(BaseModel):
    analysis: VacationAnalysis
    travel_twin: TravelTwinType = Field(
        ...,
        description=(
            'Traveller archetype. Exactly one of: '
            'Explorer, Luxury Traveler, Nature Escapist, Urban Discoverer, Adrenaline Hunter.'
        ),
    )
    search_query_for_embeddings: str = Field(
        ...,
        description=(
            'One vivid sentence about the trip scenery, vibe, and budget level '
            'for semantic similarity search over destination packages.'
        ),
    )

# ── Request / response wrappers ──────────────────────────────
class TextAnalysisRequest(BaseModel):
    text: str = Field(..., min_length=1, max_length=2000)

class VacationProfileResponse(BaseModel):
    profile: VacationProfile
    source_text: str = Field(
        ...,
        description=(
            'Text fed into the parser: original input for free text, '
            'Whisper transcript for audio, or Vision description for images.'
        ),
    )

# ── Central parser ────────────────────────────────────────────
_DV_SYSTEM_PROMPT = """
You are a travel-profile analyst. Given a description of someone's ideal vacation
(which may originate from free text, a voice transcript, or a visual scene description),
extract a structured vacation profile.

Field guidance:
- detected_vibe              : 2-4 words capturing the emotional tone.
- requested_weather          : climate or season the traveller prefers.
- pace                       : exactly one of "leisurely", "moderate", "action-packed".
- estimated_budget_level     : "low" (budget/hostels), "medium" (3-star), "high" (luxury).
- travel_twin                : EXACTLY one of —
                               Explorer | Luxury Traveler | Nature Escapist |
                               Urban Discoverer | Adrenaline Hunter
- search_query_for_embeddings: one vivid sentence capturing scenery, vibe, and budget level
                               for semantic search over destination packages.
""".strip()

def _parse_vacation_profile(source_text: str) -> VacationProfile:
    """Central parser: converts any vacation description into a VacationProfile."""
    text = source_text.strip()
    if not text:
        raise HTTPException(status_code=400, detail='source_text must not be empty.')
    try:
        completion = client.beta.chat.completions.parse(
            model=_DV_MODEL,
            messages=[
                {'role': 'system', 'content': _DV_SYSTEM_PROMPT},
                {'role': 'user',   'content': text},
            ],
            response_format=VacationProfile,
            temperature=0.2,
        )
    except OpenAIError as exc:
        raise HTTPException(status_code=503, detail=f'OpenAI error: {exc}') from exc
    choice = completion.choices[0].message
    if choice.parsed is None:
        detail = f'Model refused: {choice.refusal}' if choice.refusal else 'Empty structured response.'
        raise HTTPException(status_code=422, detail=detail)
    return choice.parsed

# ── Input handler 1 — free text ───────────────────────────────
@app.post('/dream-vacation/analyze/text')
async def dv_analyze_text(req: TextAnalysisRequest) -> VacationProfileResponse:
    """Analyse a free-text vacation description and return a structured profile."""
    sanitized = req.text.strip()
    if not sanitized:
        raise HTTPException(status_code=400, detail='text must not be empty.')
    profile = _parse_vacation_profile(sanitized)
    return VacationProfileResponse(profile=profile, source_text=sanitized)

# ── Input handler 2 — audio (Whisper → text → profile) ────────
@app.post('/dream-vacation/analyze/audio')
async def dv_analyze_audio(
    file: UploadFile = File(..., description='Voice recording (mp3/wav/webm/ogg/flac, max 25 MB).'),
) -> VacationProfileResponse:
    """Transcribe a voice recording with Whisper, then return a structured vacation profile."""
    if not file.filename:
        raise HTTPException(status_code=400, detail='Uploaded file must have a filename.')
    file_bytes = await file.read()
    if len(file_bytes) > _MAX_AUDIO_BYTES:
        raise HTTPException(status_code=413, detail='Audio file exceeds the 25 MB Whisper limit.')
    mime = file.content_type or mimetypes.guess_type(file.filename)[0] or 'audio/mpeg'
    if mime not in _ALLOWED_AUDIO_MIME:
        raise HTTPException(status_code=415, detail=f"Unsupported audio type '{mime}'.")
    audio_stream = io.BytesIO(file_bytes)
    audio_stream.name = file.filename
    try:
        transcription = client.audio.transcriptions.create(
            model=_WHISPER_MODEL,
            file=audio_stream,
            response_format='text',
        )
    except OpenAIError as exc:
        raise HTTPException(status_code=503, detail=f'Whisper transcription failed: {exc}') from exc
    transcript: str = transcription if isinstance(transcription, str) else transcription.text  # type: ignore[union-attr]
    if not transcript.strip():
        raise HTTPException(status_code=422, detail='Whisper returned empty transcription.')
    profile = _parse_vacation_profile(transcript)
    return VacationProfileResponse(profile=profile, source_text=transcript)

# ── Input handler 3 — image (GPT-4o Vision → description → profile) ─
_DV_VISION_PROMPT = (
    'You are looking at an image a traveller shared to describe their dream vacation. '
    'Describe it in 3-5 sentences focusing on: scenery and landscape, emotional vibe and mood, '
    'apparent budget/luxury level, climate and weather, pace of activity, '
    'and any geographic or cultural clues. Be specific and vivid.'
)

@app.post('/dream-vacation/analyze/image')
async def dv_analyze_image(
    file: UploadFile = File(..., description='Vacation inspiration image (jpeg/png/gif/webp, max 20 MB).'),
) -> VacationProfileResponse:
    """Describe an image with GPT-4o Vision, then return a structured vacation profile."""
    file_bytes = await file.read()
    if len(file_bytes) > _MAX_IMAGE_BYTES:
        raise HTTPException(status_code=413, detail='Image file exceeds the 20 MB limit.')
    mime = file.content_type or 'image/jpeg'
    if mime not in _ALLOWED_IMAGE_MIME:
        raise HTTPException(status_code=415, detail=f"Unsupported image type '{mime}'.")
    encoded  = base64.b64encode(file_bytes).decode('utf-8')
    data_url = f'data:{mime};base64,{encoded}'
    try:
        vision_resp = client.chat.completions.create(
            model=_DV_MODEL,
            messages=[{
                'role': 'user',
                'content': [
                    {'type': 'text',      'text': _DV_VISION_PROMPT},
                    {'type': 'image_url', 'image_url': {'url': data_url, 'detail': 'low'}},
                ],
            }],
            max_tokens=400,
            temperature=0.3,
        )
    except OpenAIError as exc:
        raise HTTPException(status_code=503, detail=f'Vision API failed: {exc}') from exc
    description: str = vision_resp.choices[0].message.content or ''
    if not description.strip():
        raise HTTPException(status_code=422, detail='Vision API returned an empty description.')
    profile = _parse_vacation_profile(description)
    return VacationProfileResponse(profile=profile, source_text=description)


# ── DREAM VACATION SEMANTIC SEARCH ───────────────────────────────────────────
# Accepts the search_query_for_embeddings string produced by the profile parser,
# ranks all indexed products by cosine similarity, and returns the top matches
# with a one-sentence AI explanation of why each package was chosen.
# ─────────────────────────────────────────────────────────────────────────────

class DreamSearchRequest(BaseModel):
    search_query: str = Field(
        ...,
        min_length=1,
        max_length=500,
        description='The search_query_for_embeddings value from a VacationProfile.',
    )
    top_k: int = Field(default=5, ge=1, le=20)

class DreamSearchMatch(BaseModel):
    product: dict
    similarity_score: float = Field(..., description='Cosine similarity (0.0–1.0).')
    why: str = Field(..., description='One sentence explaining why this package fits the query.')

class DreamSearchResponse(BaseModel):
    query: str
    matches: list[DreamSearchMatch]


def _score_and_explain_matches(
    query: str,
    matches: list[tuple[dict, float]],
) -> list[tuple[str, float]]:
    """
    Single GPT call that returns a suitability score AND a 'why' sentence per package.
    Returns a list of (why_str, relevance_float 0-1) tuples in the same order as ``matches``.
    Falls back to a neutral score on any error.
    """
    if not matches:
        return []

    package_lines = '\n'.join(
        f"{i + 1}. {p.get('name', 'Unknown')} (${p.get('price', 'N/A')}): "
        f"{p.get('description', '')}"
        for i, (p, _) in enumerate(matches)
    )
    prompt = (
        f"A traveller is looking for: \"{query}\"\n\n"
        f"The following vacation packages were returned by a vector search:\n"
        f"{package_lines}\n\n"
        f"For EACH package:\n"
        f"  1. Score its relevance: 0.0 = completely unsuitable, 1.0 = perfect match.\n"
        f"  2. Write exactly ONE sentence (max 20 words) explaining why it does or doesn't fit.\n\n"
        f"Reply ONLY with a JSON array in the same order, nothing else:\n"
        f'[{{"score": 0.95, "why": "Perfect warm beach resort with crystal-clear waters."}}, ...]'
    )
    try:
        resp = client.chat.completions.create(
            model='gpt-4o',
            messages=[{'role': 'user', 'content': prompt}],
            max_tokens=400,
            temperature=0.1,
        )
        raw = resp.choices[0].message.content or '[]'
        raw = raw.strip().removeprefix('```json').removeprefix('```').removesuffix('```').strip()
        items: list[dict] = json.loads(raw)
        result: list[tuple[str, float]] = []
        for item in items[:len(matches)]:
            result.append((
                str(item.get('why', 'Matches your travel preferences.')),
                max(0.0, min(1.0, float(item.get('score', 0.5)))),
            ))
        while len(result) < len(matches):
            result.append(('Matches your travel preferences.', 0.5))
        return result
    except (OpenAIError, json.JSONDecodeError, ValueError, KeyError, TypeError):
        return [('Matches your travel preferences.', 0.5)] * len(matches)


@app.post('/dream-vacation/search', response_model=DreamSearchResponse)
async def dv_search(req: DreamSearchRequest) -> DreamSearchResponse:
    """
    Semantic search over the product index using a vacation profile query.

    - Embeds the query with text-embedding-3-small.
    - Ranks all indexed products by cosine similarity.
    - Returns the top_k matches, each with a similarity score and a one-sentence
      AI explanation of why the package suits the traveller's request.

    Requires the product index to be populated (built at startup from products.json
    or refreshed via POST /index).
    """
    query = req.search_query.strip()
    if not query:
        raise HTTPException(status_code=400, detail='search_query must not be empty.')
    if not _product_index:
        raise HTTPException(
            status_code=503,
            detail='Product index is not yet built. Retry in a moment or call POST /index.',
        )

    # ── Embed query and score all products by cosine similarity ──
    try:
        q_vec = _embed(query)
    except OpenAIError as exc:
        raise HTTPException(status_code=503, detail=f'Embedding API error: {exc}') from exc

    all_scored: list[tuple[dict, float]] = sorted(
        (
            (item['product'], _cosine(q_vec, item['embedding']))
            for item in _product_index
        ),
        key=lambda x: x[1],
        reverse=True,
    )

    # Cast a wider net (3× top_k) so GPT can re-rank; drop only near-zero results
    candidates = [(p, s) for p, s in all_scored[:req.top_k * 3] if s > 0.15]

    if not candidates:
        return DreamSearchResponse(query=query, matches=[])

    # ── GPT scores relevance AND generates 'why' in a single call ──
    scored_explanations = _score_and_explain_matches(query, candidates)

    # ── Re-rank: GPT relevance (70%) + cosine similarity (30%) ────
    combined = [
        (product, cosine_s, why, gpt_s)
        for (product, cosine_s), (why, gpt_s)
        in zip(candidates, scored_explanations)
    ]
    combined.sort(key=lambda x: 0.3 * x[1] + 0.7 * x[3], reverse=True)

    top_matches = combined[:req.top_k]

    matches = [
        DreamSearchMatch(
            product=product,
            # Expose the blended score as the similarity_score so the UI badge
            # reflects true relevance, not raw vector distance
            similarity_score=round(0.3 * cosine_s + 0.7 * gpt_s, 4),
            why=why,
        )
        for product, cosine_s, why, gpt_s in top_matches
    ]

    return DreamSearchResponse(query=query, matches=matches)
