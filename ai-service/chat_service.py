# chat_service.py
from contextlib import asynccontextmanager
from fastapi import FastAPI, Body
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from openai import OpenAI
from dotenv import load_dotenv
import os, json, numpy as np
from datetime import date

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
