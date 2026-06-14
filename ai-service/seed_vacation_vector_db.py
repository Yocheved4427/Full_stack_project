#!/usr/bin/env python3
"""
seed_vacation_vector_db.py
==========================
One-shot script that loads products.json, upserts every product into Qdrant
via vacation_engine.py, then runs a quick end-to-end smoke test.

Usage
-----
  cd f:\\project\\ai-service
  .\.venv\Scripts\Activate.ps1
  python seed_vacation_vector_db.py

Prerequisites
-------------
  .env must contain valid values for:
    OPENAI_API_KEY   — OpenAI key
    QDRANT_URL       — e.g. https://xyz.eu-central-1.aws.cloud.qdrant.io:6333
    QDRANT_API_KEY   — cluster-level data API key

  Run create_qdrant_cloud.py --patch-env first if Qdrant is not yet configured.
"""

from __future__ import annotations

import json
import logging
import os
import sys
from typing import Any

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)-8s  %(message)s",
    datefmt="%H:%M:%S",
)
logger = logging.getLogger(__name__)

# ── Import engine (raises EnvironmentError early if .env is incomplete) ───────
try:
    from vacation_engine import (
        ensure_collection,
        parse_vacation_inspiration,
        search_vacation_packages,
        upsert_packages,
        _COLLECTION_NAME,
    )
except EnvironmentError as exc:
    sys.exit(f"\n[CONFIG ERROR] {exc}\n")

# ─────────────────────────────────────────────────────────────────────────────
# 1. Load products.json
# ─────────────────────────────────────────────────────────────────────────────

_PRODUCTS_FILE = os.path.join(os.path.dirname(__file__), "products.json")


def load_products() -> list[dict[str, Any]]:
    """
    Load products from products.json and normalise each entry into:
        {
          "id":          int,           # numeric id required by Qdrant
          "name":        str,           # destination name
          "description": str,           # full rich text used for embedding
          "raw_text":    str,           # original "text" field kept for reference
        }

    products.json uses the shape:
        { "id": "product_N", "text": "Name Location. Price: $X. Description..." }

    The "text" field already contains the name and a vivid description, so it
    is used directly as the embedding source — no further pre-processing needed.
    """
    if not os.path.isfile(_PRODUCTS_FILE):
        sys.exit(f"[ERROR] products.json not found at: {_PRODUCTS_FILE}")

    with open(_PRODUCTS_FILE, "r", encoding="utf-8") as fh:
        raw: list[dict[str, Any]] = json.load(fh)

    products: list[dict[str, Any]] = []
    for entry in raw:
        raw_id  = entry.get("id", "")
        text    = entry.get("text", "").strip()

        # Convert "product_N" → integer N for Qdrant point ID
        try:
            numeric_id = int(str(raw_id).replace("product_", ""))
        except ValueError:
            logger.warning("Skipping entry with non-parseable id: %r", raw_id)
            continue

        if not text:
            logger.warning("Skipping entry %r — empty text field.", raw_id)
            continue

        # Extract the destination name from the first sentence ("Name Location.")
        name = text.split(".")[0].strip()

        products.append(
            {
                "id":          numeric_id,
                "name":        name,
                "description": text,   # full rich text → embedding input
                "raw_text":    text,
                "source_id":   raw_id,
            }
        )

    return products


# ─────────────────────────────────────────────────────────────────────────────
# 2. Seed the collection
# ─────────────────────────────────────────────────────────────────────────────

def seed(products: list[dict[str, Any]]) -> None:
    logger.info("Target collection : %s", _COLLECTION_NAME)
    logger.info("Products to index : %d", len(products))

    logger.info("Ensuring Qdrant collection exists …")
    ensure_collection()

    logger.info("Upserting products (each requires one OpenAI embedding call) …")
    count = upsert_packages(products)
    logger.info("Done — %d products indexed.", count)


# ─────────────────────────────────────────────────────────────────────────────
# 3. End-to-end smoke test
# ─────────────────────────────────────────────────────────────────────────────

def smoke_test() -> None:
    print()
    print("=" * 65)
    print("  Smoke test: parse + search")
    print("=" * 65)

    query_text = "I want a calm vacation in August with a nice view"
    print(f"\nInput:\n  \"{query_text}\"\n")

    # Step A — parse into a structured profile
    print("── Step A: Parsing vacation inspiration ──────────────────────")
    try:
        profile = parse_vacation_inspiration(query_text)
    except Exception as exc:
        print(f"[ERROR] parse_vacation_inspiration failed: {exc}")
        return

    print(f"  travel_twin         : {profile['travel_twin']}")
    print(f"  detected_vibe       : {profile['analysis']['detected_vibe']}")
    print(f"  pace                : {profile['analysis']['pace']}")
    print(f"  budget_level        : {profile['analysis']['estimated_budget_level']}")
    search_query = profile["search_query_for_embeddings"]
    print(f"  search_query        : {search_query}")

    # Step B — semantic search
    print("\n── Step B: Semantic search (top 2 matches) ───────────────────")
    try:
        matches = search_vacation_packages(search_query, top_k=2)
    except Exception as exc:
        print(f"[ERROR] search_vacation_packages failed: {exc}")
        return

    if not matches:
        print("  No matches returned — collection may be empty.")
        return

    for rank, match in enumerate(matches, start=1):
        product = match["product"]
        score   = match["score"]
        print(f"\n  #{rank}  score={score:.4f}")
        print(f"      name : {product.get('name', '(unknown)')}")
        print(f"      text : {product.get('description', '')[:120]} …")

    print()
    print("=" * 65)
    print("  All checks passed. Run ingest.py to load policies too.")
    print("=" * 65)


# ─────────────────────────────────────────────────────────────────────────────
# Entry point
# ─────────────────────────────────────────────────────────────────────────────

def main() -> None:
    products = load_products()

    if not products:
        sys.exit("[ERROR] No products loaded — check products.json.")

    seed(products)
    smoke_test()


if __name__ == "__main__":
    main()
