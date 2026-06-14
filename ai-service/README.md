# Dreams Escapes — AI Service

A FastAPI micro-service that powers the Luna travel assistant, semantic search, and the Dream Vacation Generator for the Dreams Escapes store.  
It is called exclusively by the .NET backend — it is never exposed directly to the browser.

---

## Tech stack

- **FastAPI** — HTTP framework
- **OpenAI** — `gpt-4o` for chat, `text-embedding-3-small` for embeddings
- **Qdrant Cloud** — vector database for Dream Vacation semantic search
- **NumPy** — cosine similarity calculation for the in-memory chat/search index
- **Pydantic** — structured output models for vacation profile parsing
- **python-dotenv** — loads secrets from `.env`

---

## Project structure

```
ai-service/
├── chat_service.py          # FastAPI app — all HTTP endpoints
├── vacation_engine.py       # Dream Vacation AI engine (Qdrant-backed)
├── seed_vacation_vector_db.py  # One-shot script: seeds Qdrant with products.json
├── ingest.py                # One-time script: uploads products + policies to Qdrant
├── products.json            # Vacation package catalog (19 destinations)
├── policies.json            # Store policies (booking, cancellation, payments, etc.)
├── .env                     # Secrets (never committed — see .gitignore)
├── .gitignore
└── README.md
```

---

## Setup

### 1. Create and activate a virtual environment

```bash
python -m venv .venv
# Windows
.venv\Scripts\activate
# macOS / Linux
source .venv/bin/activate
```

### 2. Install dependencies

```bash
pip install fastapi uvicorn openai python-dotenv numpy pydantic qdrant-client
```

### 3. Create `.env`

```env
OPENAI_API_KEY=sk-...your-key-here...
STORE_NAME=Dreams Escapes
STORE_DESCRIPTION=The place to find the perfect vacation package for you

# Qdrant Cloud — required for the Dream Vacation feature
QDRANT_URL=https://xyz.eu-central-1.aws.cloud.qdrant.io:6333
QDRANT_API_KEY=your-qdrant-data-api-key
QDRANT_COLLECTION_NAME=vacation_packages   # optional, this is the default

# AI model for vacation profile parsing (optional, defaults to gpt-4o-mini)
VACATION_MODEL=gpt-4o-mini

# CORS — change to your real domain before going live
ALLOWED_ORIGIN=http://localhost:4200
```

### 4. Seed the Qdrant vector database (one-time)

```bash
python seed_vacation_vector_db.py
```

This embeds every product in `products.json` and upserts them into Qdrant. Re-run whenever the catalog changes.

### 5. Run the server

```bash
python -m uvicorn chat_service:app --port 8001 --reload
```

Interactive API docs: `http://localhost:8001/docs`

---

## How the embedding index works

### In-memory index (chat & search endpoints)

1. On startup, `chat_service.py` pre-builds an in-memory embedding index from `products.json`.
2. The .NET `EmbeddingIndexService` also POSTs up to 50 active DB products to `/index` on every .NET restart, refreshing the index with live data including **product IDs**.
3. Every `/chat` and `/search` request uses cosine similarity against those vectors — no per-query API call.

### Qdrant index (Dream Vacation endpoints)

1. `vacation_engine.py` connects to Qdrant Cloud at startup.
2. `seed_vacation_vector_db.py` seeds the collection once with products from `products.json`.
3. `POST /dream-vacation/search` embeds the AI-generated query string and queries Qdrant, returning ranked matches including their **product IDs**.
4. The .NET `VacationService` uses the returned IDs to query the SQL database directly — no name-matching fragility.

---

## Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/index` | Refresh in-memory product index (called by .NET on startup) |
| `POST` | `/chat` | Luna chat — returns reply + optional suggested search |
| `POST` | `/search` | Keyword/semantic product search |
| `POST` | `/dream-vacation/analyze/text` | Parse free-text vacation description into a structured profile |
| `POST` | `/dream-vacation/analyze/audio` | Transcribe audio → parse vacation profile |
| `POST` | `/dream-vacation/analyze/image` | Describe image → parse vacation profile |
| `POST` | `/dream-vacation/search` | Semantic search over Qdrant — returns ranked product IDs |

---

## Configuration

| Variable | Default | Description |
|---|---|---|
| `OPENAI_API_KEY` | *(required)* | Your OpenAI secret key |
| `STORE_NAME` | *(required)* | Injected into Luna's system prompt |
| `STORE_DESCRIPTION` | *(required)* | Injected into Luna's system prompt |
| `QDRANT_URL` | *(required for Dream Vacation)* | Qdrant Cloud cluster URL |
| `QDRANT_API_KEY` | *(required for Dream Vacation)* | Qdrant data API key |
| `QDRANT_COLLECTION_NAME` | `vacation_packages` | Qdrant collection name |
| `VACATION_MODEL` | `gpt-4o-mini` | OpenAI model for vacation profile parsing |
| `ALLOWED_ORIGIN` | `http://localhost:4200` | CORS origin (lock to your domain in production) |

**In-memory catalog cap:** `_MAX_INDEX_SIZE = 50` in `chat_service.py`.  
Raise this value (and `.Take(50)` in `EmbeddingIndexService.cs`) together if your catalog grows.

---

## Going live checklist

- [ ] Set `ALLOWED_ORIGIN` to your production domain in `.env`
- [ ] Rotate `OPENAI_API_KEY` — never use a dev key in production
- [ ] Set `QDRANT_URL` and `QDRANT_API_KEY` for a production Qdrant Cloud cluster
- [ ] Re-run `seed_vacation_vector_db.py` after any catalog update

- [ ] Run behind a reverse proxy (nginx / Azure API Management) — do not expose port 8001 publicly
- [ ] Set `--workers 2` (or more) in the uvicorn start command for production load
- [ ] Consider persisting the embedding index to disk so restarts don't incur re-embedding cost
