# Dreams Escapes — AI Service

A FastAPI micro-service that powers the Luna travel assistant and semantic search for the Dreams Escapes store.  
It is called exclusively by the .NET backend — it is never exposed directly to the browser.

---

## Tech stack

- **FastAPI** — HTTP framework
- **OpenAI** — `gpt-4o` for chat, `text-embedding-3-small` for embeddings
- **NumPy** — cosine similarity calculation
- **python-dotenv** — loads secrets from `.env`

---

## Project structure

```
ai-service/
├── chat_service.py   # All endpoints and business logic
├── ingest.py         # One-time script: uploads products + policies to Qdrant Cloud
├── products.json     # Vacation package catalog (19 destinations)
├── policies.json     # Store policies (booking, cancellation, payments, etc.)
├── .env              # Secrets (never committed — see .gitignore)
├── .gitignore
└── README.md
```

---

## Setup

### 1. Create and activate a virtual environment (recommended)

```bash
python -m venv .venv
# Windows
.venv\Scripts\activate
# macOS / Linux
source .venv/bin/activate
```

### 2. Install dependencies

```bash
pip install fastapi uvicorn openai python-dotenv numpy
```

### 3. Create `.env`

```env
OPENAI_API_KEY=sk-...your-key-here...
STORE_NAME=Dreams Escapes
STORE_DESCRIPTION=The place to find the perfect vacation package for you

# CORS — change to your real domain before going live
# e.g. ALLOWED_ORIGIN=https://www.dreamsescapes.com
ALLOWED_ORIGIN=http://localhost:4200
```

### 4. Run the server

```bash
python -m uvicorn chat_service:app --port 8001 --reload
```

Interactive API docs are available at `http://localhost:8001/docs`.

---

## How the embedding index works

1. On startup, the .NET `EmbeddingIndexService` fetches up to 50 active products from the database.
2. It POSTs them to `/index`.
3. The Python service embeds each product (`name + description`) using `text-embedding-3-small` and stores the vectors in memory.
4. Every chat and search request then uses cosine similarity against those vectors — no per-product API call is needed at query time.

The index is rebuilt whenever .NET restarts. For real-time catalog changes, call `/index` again from .NET after any product update.

---

## Configuration

| Variable | Default | Description |
|---|---|---|
| `OPENAI_API_KEY` | *(required)* | Your OpenAI secret key |
| `STORE_NAME` | *(required)* | Injected into Luna's system prompt |
| `STORE_DESCRIPTION` | *(required)* | Injected into Luna's system prompt |
| `ALLOWED_ORIGIN` | `http://localhost:4200` | CORS origin (lock to your domain in production) |

**Catalog cap:** `_MAX_INDEX_SIZE = 50` in `chat_service.py`.  
Raise this value (and `.Take(50)` in `EmbeddingIndexService.cs`) together if your catalog grows.

---

## Going live checklist

- [ ] Set `ALLOWED_ORIGIN` to your production domain in `.env`
- [ ] Rotate `OPENAI_API_KEY` — never use a dev key in production
- [ ] Run behind a reverse proxy (nginx / Azure API Management) — do not expose port 8001 publicly
- [ ] Set `--workers 2` (or more) in the uvicorn start command for production load
- [ ] Consider persisting the embedding index to disk so restarts don't incur re-embedding cost
