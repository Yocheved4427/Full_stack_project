"""
ingest.py — Load products.json and policies.json into Qdrant Cloud.
Run once (or whenever data changes):
    python ingest.py
"""

import os
import json
import uuid
import sys
from dotenv import load_dotenv
from qdrant_client import QdrantClient, models
from langchain_qdrant import QdrantVectorStore
from langchain_huggingface import HuggingFaceEmbeddings

load_dotenv()

QDRANT_URL    = os.environ["QDRANT_URL"]
QDRANT_API_KEY = os.environ["QDRANT_API_KEY"]
COLLECTION    = "dreams_escapes"

# ── Connect ──────────────────────────────────────────────────
print(f"[+] Connecting to Qdrant at {QDRANT_URL}...")
try:
    client = QdrantClient(url=QDRANT_URL, api_key=QDRANT_API_KEY)
except Exception as e:
    print(f"[-] Failed to connect: {e}", file=sys.stderr)
    sys.exit(1)

# ── Embeddings ───────────────────────────────────────────────
print("[+] Loading HuggingFace embeddings (all-MiniLM-L6-v2)...")
embeddings = HuggingFaceEmbeddings(model_name="sentence-transformers/all-MiniLM-L6-v2")


def init_collection():
    print(f"[+] Checking collection '{COLLECTION}'...")
    try:
        if not client.collection_exists(COLLECTION):
            print(f"[+] Creating collection '{COLLECTION}' (384 dims, Cosine)...")
            client.create_collection(
                collection_name=COLLECTION,
                vectors_config=models.VectorParams(
                    size=384,
                    distance=models.Distance.COSINE
                )
            )
        else:
            print(f"[+] Collection '{COLLECTION}' already exists.")

        # Index the 'type' field so we can filter by product vs policy at query time
        client.create_payload_index(
            collection_name=COLLECTION,
            field_name="metadata.type",
            field_schema=models.PayloadSchemaType.KEYWORD
        )
    except Exception as e:
        print(f"[-] Collection init error: {e}", file=sys.stderr)
        sys.exit(1)


def load_products() -> list[dict]:
    """Read products.json — each entry already has 'id' and 'text'."""
    path = os.path.join(os.path.dirname(__file__), "products.json")
    if not os.path.exists(path):
        print("[-] Warning: products.json not found.")
        return []
    with open(path, encoding="utf-8") as f:
        products = json.load(f)
    for p in products:
        p["metadata"] = {"type": "product", "source_id": p.get("id", "")}
    print(f"[+] Loaded {len(products)} products.")
    return products


def load_policies() -> list[dict]:
    """
    Read policies.json — it's a nested object, so we flatten each
    top-level section into a separate text chunk for better retrieval.
    """
    path = os.path.join(os.path.dirname(__file__), "policies.json")
    if not os.path.exists(path):
        print("[-] Warning: policies.json not found.")
        return []
    with open(path, encoding="utf-8") as f:
        data = json.load(f)

    chunks = []
    store  = data.get("store", "Dreams Escapes")

    section_labels = {
        "booking":          "Booking Policy",
        "cancellation":     "Cancellation Policy",
        "payment":          "Payment Policy",
        "changes":          "Change & Amendment Policy",
        "inclusions":       "What's Included",
        "age_policy":       "Age Policy",
        "travel_insurance": "Travel Insurance Policy",
        "complaints":       "Complaints Policy",
        "loyalty":          "Loyalty Programme",
    }

    for key, label in section_labels.items():
        section = data.get(key)
        if not section:
            continue
        # Serialise the section as readable text
        text = f"{store} — {label}:\n{json.dumps(section, indent=2)}"
        chunks.append({
            "id":       f"policy_{key}",
            "text":     text,
            "metadata": {"type": "policy", "section": key}
        })

    print(f"[+] Loaded {len(chunks)} policy chunks.")
    return chunks


def ingest():
    init_collection()

    items = load_products() + load_policies()
    if not items:
        print("[-] No data to ingest. Exiting.")
        return

    db = QdrantVectorStore(
        client=client,
        collection_name=COLLECTION,
        embedding=embeddings
    )

    texts  = [x["text"]     for x in items]
    metas  = [x["metadata"] for x in items]
    ids    = [str(uuid.uuid4()) for _ in items]

    print(f"[+] Ingesting {len(items)} items into '{COLLECTION}'...")
    try:
        db.add_texts(texts=texts, metadatas=metas, ids=ids)
        print(f"[+] Done! {len(items)} items ingested ({len(load_products())} products + {len(load_policies())} policy chunks).")
    except Exception as e:
        print(f"[-] Ingestion error: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    ingest()
