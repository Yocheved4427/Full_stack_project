namespace DTOs
{
    // ── Inbound DTO ──────────────────────────────────────────────────────────
    // NOTE: IFormFile cannot live here (DTOs project has no ASP.NET reference).
    // The controller binds the files directly via [FromForm] and passes raw bytes
    // to the service. This DTO carries only the text field for JSON binding.
    public class VacationBuildRequest
    {
        /// <summary>Free-text vacation description (e.g. "calm August trip, nice view").</summary>
        public string? Text { get; set; }
    }

    // ── Outbound DTOs ────────────────────────────────────────────────────────

    public record VacationAnalysisDTO(
        string DetectedVibe,
        string RequestedWeather,
        string Pace,
        string EstimatedBudgetLevel);

    public record VacationBuildResponseDTO(
        /// <summary>Structured vibe/budget/pace profile returned by the AI.</summary>
        VacationAnalysisDTO Analysis,
        /// <summary>Traveller archetype (e.g. "Nature Escapist").</summary>
        string TravelTwin,
        /// <summary>The raw query string used for the embedding search.</summary>
        string SearchQuery,
        /// <summary>Top matched products fetched from the database.</summary>
        List<ProductDTO> RecommendedPackages,
        /// <summary>One-sentence AI explanation for each recommended package (same order).</summary>
        List<string> WhyRecommended,
        /// <summary>Similarity scores for each recommended package (same order).</summary>
        List<double> SimilarityScores);

    // ── What-If Simulator DTOs ────────────────────────────────────────────────

    public record SimulatorRequestDTO
    {
        /// <summary>Desired luxury tier: 1 = budget hostel, 5 = ultra-luxury resort.</summary>
        public int LuxuryLevel { get; init; }      // 1–5

        /// <summary>Nature preference: 1 = pure city/urban, 5 = wilderness/nature.</summary>
        public int NatureVibe { get; init; }        // 1–5

        /// <summary>Hard upper price limit per person. 0 = no limit.</summary>
        public decimal BudgetLimit { get; init; }  // ≥ 0

        /// <summary>Minimum number of distinct activity / attraction types expected.</summary>
        public int AttractionCount { get; init; }  // ≥ 0
    }

    public record SimulatorResultItemDTO(
        ProductDTO Product,
        double     Score,
        string     MatchReason);

    public record SimulatorResponseDTO(
        List<SimulatorResultItemDTO> Results,
        int TotalConsidered,
        int TotalExcludedByBudget);
}
