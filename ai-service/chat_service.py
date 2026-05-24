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
        # products.json uses 'text' field; live /index uses 'name'+'description'
        text = p.get('text') or f"{p.get('name', '')} — {p.get('description', '')}"
        _product_index.append({
            'product':   p,
            'embedding': _embed(text)
        })
    print(f'[index] Pre-built from products.json: {len(_product_index)} products')
    return float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-9))

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

Your tone is warm, enthusiastic, and inspiring.
You speak like a well-travelled friend who genuinely wants to help people find their dream getaway — not like a salesperson pushing packages.

Rules you must ALWAYS follow:
1. Before recommending anything, always ask for the customer's budget per person AND travel dates — never skip this.
2. Only recommend packages that are available during the customer's requested travel period. If a package is not available for those dates, say so honestly and suggest an alternative that is.
3. Always mention that prices vary by season — if the customer hasn't specified dates yet, remind them that pricing depends on the travel period.
4. Never recommend a destination or package that Dreams Escapes does not carry — do not invent packages, prices, or availability.
5. Always end every reply with exactly one follow-up question to keep the conversation going.
6. If the user mentions a competitor (e.g. Expedia, Booking.com, TripAdvisor), say: "I only know our own packages at Dreams Escapes, but I'd love to help you find something perfect here!"
7. Never discuss politics, safety warnings, visa requirements, or anything outside of helping the customer plan a vacation.
8. Keep every reply to 3-4 sentences maximum — be warm and concise, never overwhelming.
9. If the customer seems stressed or unsure, reassure them first — vacation planning should feel exciting, not stressful.

Output format rules:
- When comparing two vacation packages, use this exact structure:
  Option A: [name] - [one sentence benefit]
  Option B: [name] - [one sentence benefit]
  My pick: [which one and why, one sentence]
- When listing multiple destinations, use a simple bullet list — one destination per line.
- Never use markdown headers, pricing tables, or travel-industry jargon.

Example exchange (match this exact length, tone, and question style):
User: do you have anything warm for next month?
Luna: Oh, you're definitely going to love what we have for warm-weather escapes! Two of our most popular right now are the Bali Serenity Escape and the Maldives Beach Retreat — both are stunning for sunshine and relaxation. Could I ask what your budget per person is so I can point you to the best fit?
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
            stock = 'available this month' if p.get('inStock') else 'not available this month'
            price = p.get('price', 'N/A')
            line = f"- {p['name']} (${price}) [{stock}]: {p.get('description', '')}"
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
    return {'reply': response.choices[0].message.content}
