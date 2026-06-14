"""
vacation_engine.py
==================
Core AI engine for the Dream Vacation Generator.

Responsibilities
----------------
- Initialize OpenAI and Qdrant Cloud clients from environment variables.
- Expose `parse_vacation_inspiration(user_input)` → structured dict.
- Expose `search_vacation_packages(query, top_k)` → ranked matches from Qdrant.

This module is intentionally framework-agnostic (no FastAPI / Flask imports)
so it can be used from chat_service.py, a CLI script, or a test suite.

Environment variables (loaded from .env automatically)
------------------------------------------------------
  OPENAI_API_KEY   — required
  QDRANT_URL       — required  (e.g. https://xyz.eu-central-1.aws.cloud.qdrant.io:6333)
  QDRANT_API_KEY   — required  (cluster-level data API key)
  QDRANT_COLLECTION_NAME — optional, defaults to "vacation_packages"
  VACATION_MODEL   — optional, defaults to "gpt-4o-mini"
"""

from __future__ import annotations

import logging
import os
from typing import Any, Literal

from dotenv import load_dotenv
from openai import OpenAI, OpenAIError
from pydantic import BaseModel, Field
from qdrant_client import QdrantClient
from qdrant_client.http.exceptions import UnexpectedResponse
from qdrant_client.models import Distance, PointStruct, VectorParams

load_dotenv()

logger = logging.getLogger(__name__)

# ─────────────────────────────────────────────────────────────────────────────
# Type aliases used across the module
# ─────────────────────────────────────────────────────────────────────────────

BudgetLevel   = Literal["low", "medium", "high"]
TravelTwin    = Literal[
    "Explorer",
    "Luxury Traveler",
    "Nature Escapist",
    "Urban Discoverer",
    "Adrenaline Hunter",
]
Pace          = Literal["leisurely", "moderate", "action-packed"]

# ─────────────────────────────────────────────────────────────────────────────
# Pydantic models — used as OpenAI Structured Output response_format
# ─────────────────────────────────────────────────────────────────────────────

class VacationAnalysis(BaseModel):
    detected_vibe: str = Field(
        ...,
        description="2-4 words capturing the overall emotional vibe "
                    "(e.g. 'quiet luxury', 'rustic nature', 'fast-paced city').",
    )
    requested_weather: str = Field(
        ...,
        description="Preferred climate or season (e.g. 'warm and sunny', 'cold alpine').",
    )
    pace: Pace = Field(
        ...,
        description="Exactly one of: leisurely, moderate, action-packed.",
    )
    estimated_budget_level: BudgetLevel = Field(
        ...,
        description="low = budget/hostels, medium = 3-star hotels, high = luxury/5-star.",
    )


class VacationProfile(BaseModel):
    analysis: VacationAnalysis
    travel_twin: TravelTwin = Field(
        ...,
        description=(
            "Traveller archetype. Exactly one of: "
            "Explorer | Luxury Traveler | Nature Escapist | "
            "Urban Discoverer | Adrenaline Hunter."
        ),
    )
    search_query_for_embeddings: str = Field(
        ...,
        description=(
            "One vivid sentence capturing scenery, atmosphere, and budget level, "
            "optimized for semantic similarity search over destination packages."
        ),
    )


# ─────────────────────────────────────────────────────────────────────────────
# Configuration — read once at import time
# ─────────────────────────────────────────────────────────────────────────────

_OPENAI_API_KEY   = os.getenv("OPENAI_API_KEY", "")
_QDRANT_URL       = os.getenv("QDRANT_URL", "")
_QDRANT_API_KEY   = os.getenv("QDRANT_API_KEY", "")
_COLLECTION_NAME  = os.getenv("QDRANT_COLLECTION_NAME", "vacation_packages")
_VACATION_MODEL   = os.getenv("VACATION_MODEL", "gpt-4o-mini")
_EMBEDDING_MODEL  = "text-embedding-3-small"
_EMBEDDING_DIM    = 1536  # dimensionality of text-embedding-3-small

_SYSTEM_PROMPT = """\
You are a travel-profile analyst. Given a description of someone's ideal vacation,
extract a structured vacation profile.

Field guidance:
  detected_vibe              → 2-4 words for the emotional tone.
  requested_weather          → preferred climate or season.
  pace                       → exactly one of: leisurely | moderate | action-packed.
  estimated_budget_level     → low (budget) | medium (3-star) | high (luxury).
  travel_twin                → exactly one of:
                               Explorer | Luxury Traveler | Nature Escapist |
                               Urban Discoverer | Adrenaline Hunter.
  search_query_for_embeddings→ one vivid sentence about scenery, vibe, and budget
                               for semantic search over destination packages.
""".strip()


# ─────────────────────────────────────────────────────────────────────────────
# Client initialization
# ─────────────────────────────────────────────────────────────────────────────

def _require_env(name: str, value: str) -> str:
    """Raise a clear error if a required environment variable is missing."""
    if not value or value.startswith("your-") or "<" in value:
        raise EnvironmentError(
            f"Environment variable '{name}' is not set or still contains a placeholder.\n"
            f"  Set it in ai-service/.env before using {__name__}."
        )
    return value


def _build_openai_client() -> OpenAI:
    """Create and return a configured OpenAI client."""
    api_key = _require_env("OPENAI_API_KEY", _OPENAI_API_KEY)
    return OpenAI(api_key=api_key)


def _build_qdrant_client() -> QdrantClient:
    """
    Create and return a QdrantClient connected to Qdrant Cloud.

    The API key is passed via the `api_key` parameter, which the SDK
    automatically includes as the `api-key` header on every request —
    this is the correct authentication mechanism for secured Qdrant Cloud
    clusters (both REST and gRPC).
    """
    url     = _require_env("QDRANT_URL", _QDRANT_URL)
    api_key = _require_env("QDRANT_API_KEY", _QDRANT_API_KEY)

    # prefer_grpc=False keeps things simple; set True for high-throughput writes.
    return QdrantClient(
        url=url,
        api_key=api_key,
        prefer_grpc=False,
        timeout=20,
    )


# Lazy singletons — initialized on first use so import errors are surfaced
# at call-time rather than module load-time (useful in test environments).
_openai_client:  OpenAI | None      = None
_qdrant_client:  QdrantClient | None = None


def get_openai() -> OpenAI:
    """Return the shared OpenAI client, initializing it on first call."""
    global _openai_client
    if _openai_client is None:
        _openai_client = _build_openai_client()
    return _openai_client


def get_qdrant() -> QdrantClient:
    """Return the shared QdrantClient, initializing it on first call."""
    global _qdrant_client
    if _qdrant_client is None:
        _qdrant_client = _build_qdrant_client()
    return _qdrant_client


# ─────────────────────────────────────────────────────────────────────────────
# Embedding helper
# ─────────────────────────────────────────────────────────────────────────────

def _embed(text: str) -> list[float]:
    """Return the embedding vector for `text` using text-embedding-3-small."""
    response = get_openai().embeddings.create(
        model=_EMBEDDING_MODEL,
        input=text.strip(),
    )
    return response.data[0].embedding


# ─────────────────────────────────────────────────────────────────────────────
# Public API — 1: parse_vacation_inspiration
# ─────────────────────────────────────────────────────────────────────────────

def parse_vacation_inspiration(user_input: str) -> dict[str, Any]:
    """
    Convert free-text vacation inspiration into a structured profile dict.

    Parameters
    ----------
    user_input : str
        Any natural-language vacation description (text, audio transcript,
        or Vision-generated image description).

    Returns
    -------
    dict with the shape:
        {
          "analysis": {
            "detected_vibe": str,
            "requested_weather": str,
            "pace": "leisurely" | "moderate" | "action-packed",
            "estimated_budget_level": "low" | "medium" | "high"
          },
          "travel_twin": "Explorer" | "Luxury Traveler" | "Nature Escapist"
                       | "Urban Discoverer" | "Adrenaline Hunter",
          "search_query_for_embeddings": str
        }

    Raises
    ------
    ValueError
        If user_input is blank.
    OpenAIError
        If the OpenAI API call fails (network, quota, etc.).
    RuntimeError
        If the model returns an empty or refused structured response.
    """
    text = user_input.strip()
    if not text:
        raise ValueError("user_input must not be empty.")

    logger.debug("Parsing vacation inspiration (model=%s, chars=%d)", _VACATION_MODEL, len(text))

    try:
        completion = get_openai().beta.chat.completions.parse(
            model=_VACATION_MODEL,
            messages=[
                {"role": "system", "content": _SYSTEM_PROMPT},
                {"role": "user",   "content": text},
            ],
            response_format=VacationProfile,
            temperature=0.2,
        )
    except OpenAIError as exc:
        logger.error("OpenAI structured output failed: %s", exc)
        raise

    message = completion.choices[0].message

    if message.parsed is None:
        reason = message.refusal or "Empty structured response from model."
        raise RuntimeError(f"Model refused or returned empty output: {reason}")

    profile: VacationProfile = message.parsed
    return profile.model_dump()


# ─────────────────────────────────────────────────────────────────────────────
# Public API — 2: ensure_collection
# ─────────────────────────────────────────────────────────────────────────────

def ensure_collection(collection_name: str = _COLLECTION_NAME) -> None:
    """
    Create the Qdrant collection if it doesn't already exist.

    Call this once before ingesting documents (e.g., from ingest.py).
    Safe to call multiple times — it's idempotent.
    """
    qdrant = get_qdrant()
    existing = {c.name for c in qdrant.get_collections().collections}

    if collection_name in existing:
        logger.info("Collection '%s' already exists — skipping creation.", collection_name)
        return

    qdrant.create_collection(
        collection_name=collection_name,
        vectors_config=VectorParams(
            size=_EMBEDDING_DIM,
            distance=Distance.COSINE,
        ),
    )
    logger.info("Created Qdrant collection '%s' (dim=%d, cosine).", collection_name, _EMBEDDING_DIM)


# ─────────────────────────────────────────────────────────────────────────────
# Public API — 3: upsert_packages
# ─────────────────────────────────────────────────────────────────────────────

def upsert_packages(
    packages: list[dict[str, Any]],
    collection_name: str = _COLLECTION_NAME,
) -> int:
    """
    Embed and upsert a list of product/package dicts into Qdrant.

    Each dict must have at least a "name" field and ideally a "description".
    The text used for embedding is:  "{name}. {description}"

    Returns the number of points upserted.
    """
    ensure_collection(collection_name)
    qdrant = get_qdrant()

    points: list[PointStruct] = []
    for i, pkg in enumerate(packages):
        name        = pkg.get("name", "")
        description = pkg.get("description", "")
        embed_text  = f"{name}. {description}".strip()

        vector = _embed(embed_text)
        points.append(
            PointStruct(
                id=pkg.get("id", i),
                vector=vector,
                payload=pkg,
            )
        )

    qdrant.upsert(collection_name=collection_name, points=points)
    logger.info("Upserted %d packages into '%s'.", len(points), collection_name)
    return len(points)


# ─────────────────────────────────────────────────────────────────────────────
# Public API — 4: search_vacation_packages
# ─────────────────────────────────────────────────────────────────────────────

def search_vacation_packages(
    query: str,
    top_k: int = 5,
    collection_name: str = _COLLECTION_NAME,
) -> list[dict[str, Any]]:
    """
    Perform a semantic search over the Qdrant collection.

    Parameters
    ----------
    query : str
        The `search_query_for_embeddings` value from `parse_vacation_inspiration`.
    top_k : int
        Maximum number of results to return.
    collection_name : str
        Target Qdrant collection.

    Returns
    -------
    list of dicts, each containing:
        {
          "product":  <original package payload dict>,
          "score":    <cosine similarity float 0-1>,
        }

    Raises
    ------
    UnexpectedResponse
        If the Qdrant collection does not exist or the request fails.
    """
    if not query.strip():
        raise ValueError("query must not be empty.")

    query_vector = _embed(query)
    qdrant       = get_qdrant()

    try:
        results = qdrant.search(
            collection_name=collection_name,
            query_vector=query_vector,
            limit=top_k,
            with_payload=True,
        )
    except UnexpectedResponse as exc:
        logger.error("Qdrant search failed: %s", exc)
        raise

    return [
        {
            "product": hit.payload,
            "score":   round(hit.score, 6),
        }
        for hit in results
    ]


# ─────────────────────────────────────────────────────────────────────────────
# Quick smoke-test — run directly: python vacation_engine.py
# ─────────────────────────────────────────────────────────────────────────────

if __name__ == "__main__":
    import json

    logging.basicConfig(level=logging.INFO, format="%(levelname)s  %(message)s")

    sample = "I want a calm week in the mountains, cold air, not too expensive, mostly hiking."
    print(f"Input: {sample}\n")

    try:
        profile = parse_vacation_inspiration(sample)
        print("── Vacation Profile ──────────────────────────────────────────")
        print(json.dumps(profile, indent=2, ensure_ascii=False))
    except EnvironmentError as e:
        print(f"[CONFIG ERROR] {e}")
    except OpenAIError as e:
        print(f"[OPENAI ERROR] {e}")
    except Exception as e:
        print(f"[ERROR] {e}")
