using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using DTOs;
using Microsoft.Extensions.Logging;
using Repositories;

namespace Services
{
    /// <summary>
    /// Orchestrates the Dream Vacation feature:
    ///   1. Forwards the user's input (text / audio / image) to the Python AI service.
    ///   2. Calls the Python semantic-search endpoint with the generated embedding query.
    ///   3. Resolves matched product names against the database and returns a unified DTO.
    ///
    /// Registered as a typed HttpClient in Program.cs:
    ///   builder.Services.AddHttpClient&lt;IVacationService, VacationService&gt;(c =&gt;
    ///       c.BaseAddress = new Uri("http://localhost:8001/"));
    /// </summary>
    public class VacationService : IVacationService
    {
        private readonly HttpClient                      _http;
        private readonly IProductsRepository             _productsRepo;
        private readonly IMapper                          _mapper;
        private readonly ILogger<VacationService>         _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        };

        public VacationService(
            HttpClient                  http,
            IProductsRepository         productsRepo,
            IMapper                     mapper,
            ILogger<VacationService>    logger)
        {
            _http         = http;
            _productsRepo = productsRepo;
            _mapper       = mapper;
            _logger       = logger;
        }

        // ── Public entry point ──────────────────────────────────────────────

        public async Task<VacationBuildResponseDTO> BuildVacationAsync(
            string?  text,
            byte[]?  audioBytes,   string? audioFileName, string? audioContentType,
            byte[]?  imageBytes,   string? imageContentType,
            CancellationToken ct = default)
        {
            // ── Step 1: call the correct Python analyse endpoint ─────────────
            PyProfileResponse profileResp;

            if (!string.IsNullOrWhiteSpace(text))
            {
                profileResp = await AnalyzeTextAsync(text, ct);
            }
            else if (audioBytes is { Length: > 0 })
            {
                profileResp = await AnalyzeAudioAsync(
                    audioBytes, audioFileName ?? "recording.mp3", audioContentType ?? "audio/mpeg", ct);
            }
            else if (imageBytes is { Length: > 0 })
            {
                profileResp = await AnalyzeImageAsync(
                    imageBytes, imageContentType ?? "image/jpeg", ct);
            }
            else
            {
                throw new ArgumentException(
                    "At least one input (text, audio, or image) must be provided.");
            }

            var profile     = profileResp.Profile;
            var searchQuery = profile.SearchQueryForEmbeddings;

            // ── Step 2: semantic search over the product index ───────────────
            var searchResp = await SearchPackagesAsync(searchQuery, topK: 5, ct);

            // ── Step 3: fetch entire active catalogue once ───────────────────
            var (allProducts, _) = await _productsRepo.GetProducts(
                position: 1, skip: 200,
                categoryIds: [], description: null,
                maxPrice: null, minPrice: null);

            var activeProducts = allProducts.Where(p => p.IsActive).ToList();

            // ── Step 4: resolve each AI match to a DB product ────────────────
            // Primary:  match by numeric id (present after EmbeddingIndexService refresh).
            // Fallback: match by product name (used when Python index was rebuilt from
            //           products.json on hot-reload before .NET refreshed the index).
            var nameLookup = activeProducts
                .ToDictionary(
                    p => (p.ProductName ?? string.Empty).Trim().ToLowerInvariant(),
                    p => p,
                    StringComparer.OrdinalIgnoreCase);

            var idLookup = activeProducts
                .Where(p => p.ProductId > 0)
                .ToDictionary(p => p.ProductId);

            var recommendedDTOs = new List<ProductDTO>();
            var whyList         = new List<string>();
            var scoreList       = new List<double>();

            foreach (var match in searchResp.Matches)
            {
                Entities.Product? entity = null;

                // Try ID first
                var rawId = match.Product.GetValueOrDefault("id");
                if (rawId is JsonElement idElem && idElem.ValueKind == JsonValueKind.Number)
                {
                    int id = idElem.GetInt32();
                    idLookup.TryGetValue(id, out entity);
                }

                // Fall back to name matching
                if (entity is null)
                {
                    var rawName = match.Product.GetValueOrDefault("name");
                    var name    = rawName is JsonElement nameElem
                        ? (nameElem.GetString() ?? string.Empty).Trim()
                        : rawName?.ToString()?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(name))
                        nameLookup.TryGetValue(name.ToLowerInvariant(), out entity);
                }

                if (entity is null) continue;

                recommendedDTOs.Add(_mapper.Map<ProductDTO>(entity));
                whyList.Add(match.Why);
                scoreList.Add(match.SimilarityScore);
            }

            if (recommendedDTOs.Count == 0)
                _logger.LogWarning(
                    "Zero packages matched after ID + name resolution. " +
                    "Python returned {Count} matches.",
                    searchResp.Matches.Count);

            // ── Step 5: assemble the unified response ────────────────────────
            return new VacationBuildResponseDTO(
                Analysis: new VacationAnalysisDTO(
                    DetectedVibe:          profile.Analysis.DetectedVibe,
                    RequestedWeather:      profile.Analysis.RequestedWeather,
                    Pace:                  profile.Analysis.Pace,
                    EstimatedBudgetLevel:  profile.Analysis.EstimatedBudgetLevel),
                TravelTwin:           profile.TravelTwin,
                SearchQuery:          searchQuery,
                RecommendedPackages:  recommendedDTOs,
                WhyRecommended:       whyList,
                SimilarityScores:     scoreList);
        }

        // ── Simulator ────────────────────────────────────────────────────────

        public async Task<SimulatorResponseDTO> SimulateAsync(
            SimulatorRequestDTO req,
            CancellationToken   ct = default)
        {
            // Validate slider ranges
            if (req.LuxuryLevel    is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(req.LuxuryLevel),    "Must be 1–5.");
            if (req.NatureVibe     is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(req.NatureVibe),     "Must be 1–5.");
            if (req.BudgetLimit    < 0)            throw new ArgumentOutOfRangeException(nameof(req.BudgetLimit),    "Must be ≥ 0.");
            if (req.AttractionCount < 0)           throw new ArgumentOutOfRangeException(nameof(req.AttractionCount),"Must be ≥ 0.");

            // Fetch entire active catalogue (no price/category filter — simulator controls that)
            var (products, _) = await _productsRepo.GetProducts(
                position: 1, skip: 200,
                categoryIds: [], description: null,
                maxPrice: null, minPrice: null);

            int currentMonth  = DateTime.Now.Month;
            int totalConsidered = products.Count;
            int excludedByBudget = 0;

            var scored = new List<(Entities.Product product, decimal price, double score)>();

            foreach (var p in products)
            {
                decimal price = SimulatorScorer.GetEffectivePrice(p, currentMonth);

                // Hard budget filter
                if (req.BudgetLimit > 0 && price > req.BudgetLimit)
                {
                    excludedByBudget++;
                    continue;
                }

                double score = SimulatorScorer.Score(
                    p, req.LuxuryLevel, req.NatureVibe,
                    req.BudgetLimit, req.AttractionCount, currentMonth);

                scored.Add((p, price, score));
            }

            // Sort descending by score, return top 10
            var results = scored
                .OrderByDescending(x => x.score)
                .Take(10)
                .Select(x => new SimulatorResultItemDTO(
                    Product:     _mapper.Map<ProductDTO>(x.product),
                    Score:       Math.Round(x.score, 4),
                    MatchReason: SimulatorScorer.BuildReason(
                        x.product, req.LuxuryLevel, req.NatureVibe,
                        req.AttractionCount, x.price, req.BudgetLimit)))
                .ToList();

            return new SimulatorResponseDTO(
                Results:              results,
                TotalConsidered:      totalConsidered,
                TotalExcludedByBudget: excludedByBudget);
        }

        // ── Private: Python AI call helpers ──────────────────────────────────

        private async Task<PyProfileResponse> AnalyzeTextAsync(
            string text, CancellationToken ct)
        {
            var response = await _http.PostAsJsonAsync(
                "dream-vacation/analyze/text",
                new { text },
                _jsonOpts, ct);

            return await EnsureAndDeserializeAsync<PyProfileResponse>(response, ct);
        }

        private async Task<PyProfileResponse> AnalyzeAudioAsync(
            byte[] bytes, string fileName, string contentType, CancellationToken ct)
        {
            using var content = new MultipartFormDataContent();
            var fileContent   = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            var response = await _http.PostAsync(
                "dream-vacation/analyze/audio", content, ct);

            return await EnsureAndDeserializeAsync<PyProfileResponse>(response, ct);
        }

        private async Task<PyProfileResponse> AnalyzeImageAsync(
            byte[] bytes, string contentType, CancellationToken ct)
        {
            using var content = new MultipartFormDataContent();
            var fileContent   = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", "image.jpg");

            var response = await _http.PostAsync(
                "dream-vacation/analyze/image", content, ct);

            return await EnsureAndDeserializeAsync<PyProfileResponse>(response, ct);
        }

        private async Task<PySearchResponse> SearchPackagesAsync(
            string query, int topK, CancellationToken ct)
        {
            var response = await _http.PostAsJsonAsync(
                "dream-vacation/search",
                new { search_query = query, top_k = topK },
                _jsonOpts, ct);

            return await EnsureAndDeserializeAsync<PySearchResponse>(response, ct);
        }

        private static async Task<T> EnsureAndDeserializeAsync<T>(
            HttpResponseMessage response, CancellationToken ct)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"AI service returned {(int)response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOpts, ct);
            return result ?? throw new InvalidOperationException(
                $"AI service returned an empty response for type {typeof(T).Name}.");
        }

        // ── Private Python response shapes ───────────────────────────────────
        // Internal-only; not exposed outside the service.

        private sealed class PyProfileResponse
        {
            public PyVacationProfile Profile    { get; set; } = null!;
            public string            SourceText { get; set; } = string.Empty;
        }

        private sealed class PyVacationProfile
        {
            public PyAnalysis Analysis                     { get; set; } = null!;
            public string     TravelTwin                  { get; set; } = string.Empty;
            public string     SearchQueryForEmbeddings    { get; set; } = string.Empty;
        }

        private sealed class PyAnalysis
        {
            public string DetectedVibe           { get; set; } = string.Empty;
            public string RequestedWeather       { get; set; } = string.Empty;
            public string Pace                   { get; set; } = string.Empty;
            public string EstimatedBudgetLevel   { get; set; } = string.Empty;
        }

        private sealed class PySearchResponse
        {
            public string          Query   { get; set; } = string.Empty;
            public List<PyMatch>   Matches { get; set; } = [];
        }

        private sealed class PyMatch
        {
            public Dictionary<string, object?> Product         { get; set; } = [];
            public double                      SimilarityScore { get; set; }
            public string                      Why             { get; set; } = string.Empty;
        }
    }

    // ── Simulator scoring (static helpers, no I/O) ───────────────────────────
    // Kept outside the main class so they are easy to unit-test independently.

    internal static class SimulatorScorer
    {
        // Keywords that signal a nature-oriented destination
        private static readonly string[] _natureKeywords =
        [
            "mountain","ski","skiing","alps","fjord","arctic","forest","beach",
            "island","nature","lake","coast","coral","jungle","wildlife",
            "trekking","hiking","canyon","volcano","glacier","river","waterfall",
            "safari","desert","rainforest","national park","hot spring"
        ];

        // Keywords that signal a rich activities / attractions offering
        private static readonly string[] _attractionKeywords =
        [
            "museum","gallery","monument","cathedral","castle","ruins","palace",
            "market","festival","concert","theater","show","zoo","aquarium",
            "tour","cruise","sport","adventure","diving","kayak","rafting",
            "cable car","safari","tasting","workshop","nightlife","temple",
            "exhibition","carnival","cultural","historic","heritage","spa"
        ];

        /// <summary>Effective price for the given calendar month.</summary>
        public static decimal GetEffectivePrice(Entities.Product p, int month)
        {
            var cfg = p.ProductMonthConfigs.FirstOrDefault(m => m.MonthNumber == month);
            return cfg?.SpecialPrice > 0 ? cfg.SpecialPrice.Value : p.Price ?? 0m;
        }

        /// <summary>
        /// Composite score in [0.0, 1.0].
        /// Weights: luxury 35 % | nature 30 % | attractions 20 % | budget efficiency 15 %.
        /// </summary>
        public static double Score(
            Entities.Product p,
            int     luxuryLevel,
            int     natureVibe,
            decimal budgetLimit,
            int     attractionCount,
            int     currentMonth)
        {
            decimal price = GetEffectivePrice(p, currentMonth);

            // ── 1. Luxury match ───────────────────────────────────────────────
            // Map price to a 1-5 tier, then penalise distance from requested level
            int priceTier = price switch
            {
                < 500m  => 1,
                < 800m  => 2,
                < 1100m => 3,
                < 1500m => 4,
                _       => 5
            };
            double luxuryScore = 1.0 - Math.Abs(priceTier - luxuryLevel) / 4.0;

            // ── 2. Nature vibe match ──────────────────────────────────────────
            string full = $"{p.ProductName} {p.Description}".ToLowerInvariant();
            int natureHits = _natureKeywords.Count(k => full.Contains(k));
            // naturalness: 0 = pure city, 1 = maximum nature (capped at 6 hits)
            double naturalness     = Math.Min(natureHits / 6.0, 1.0);
            double requestedNature = (natureVibe - 1) / 4.0;
            double natureScore     = 1.0 - Math.Abs(naturalness - requestedNature);

            // ── 3. Attraction count match ─────────────────────────────────────
            int    actHits         = _attractionKeywords.Count(k => full.Contains(k));
            double attractionScore = attractionCount == 0
                ? 1.0   // no preference → neutral
                : Math.Min(actHits / (double)attractionCount, 1.0);

            // ── 4. Budget efficiency ──────────────────────────────────────────
            // Reward staying within budget; penalise if already filtered out
            double budgetScore = budgetLimit > 0
                ? Math.Max(0.0, (double)((budgetLimit - price) / budgetLimit))
                : 1.0;

            return (0.35 * luxuryScore)
                 + (0.30 * natureScore)
                 + (0.20 * attractionScore)
                 + (0.15 * budgetScore);
        }

        /// <summary>Builds a human-readable explanation of why a package was chosen.</summary>
        public static string BuildReason(
            Entities.Product p,
            int     luxuryLevel,
            int     natureVibe,
            int     attractionCount,
            decimal effectivePrice,
            decimal budgetLimit)
        {
            var parts = new List<string>();

            if (budgetLimit > 0)
                parts.Add($"fits your ${budgetLimit:N0} budget at ${effectivePrice:N0}/person");

            int priceTier = effectivePrice switch
            {
                < 500m  => 1, < 800m => 2, < 1100m => 3, < 1500m => 4, _ => 5
            };
            if (Math.Abs(priceTier - luxuryLevel) <= 1)
                parts.Add(luxuryLevel >= 4
                    ? "matches your luxury expectations"
                    : "fits your value-for-money preference");

            string full      = $"{p.ProductName} {p.Description}".ToLowerInvariant();
            bool   hasNature = _natureKeywords.Any(k => full.Contains(k));
            if (natureVibe >= 4 && hasNature)
                parts.Add("offers the natural scenery you're after");
            else if (natureVibe <= 2 && !hasNature)
                parts.Add("suits your urban/cultural preference");

            int actHits = _attractionKeywords.Count(k => full.Contains(k));
            if (attractionCount > 0 && actHits >= attractionCount)
                parts.Add($"provides at least {attractionCount} activity type(s)");

            if (parts.Count == 0)
                parts.Add("aligns with your overall preferences");

            var reason = string.Join(", ", parts) + ".";
            return char.ToUpper(reason[0]) + reason[1..];
        }
    }
}
