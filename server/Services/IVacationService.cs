using DTOs;

namespace Services
{
    public interface IVacationService
    {
        /// <summary>
        /// Analyse a vacation profile from one of three input types, then perform a semantic
        /// search over the product catalogue and return matched packages with AI explanations.
        /// Exactly one of the three input groups must be provided.
        /// </summary>
        /// <param name="text">Free-text vacation description.</param>
        /// <param name="audioBytes">Raw bytes of a voice recording file.</param>
        /// <param name="audioFileName">Original filename (used by Whisper to infer format).</param>
        /// <param name="audioContentType">MIME type of the audio file.</param>
        /// <param name="imageBytes">Raw bytes of an inspiration image.</param>
        /// <param name="imageContentType">MIME type of the image file.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<VacationBuildResponseDTO> BuildVacationAsync(
            string?  text,
            byte[]?  audioBytes,     string? audioFileName, string? audioContentType,
            byte[]?  imageBytes,     string? imageContentType,
            CancellationToken ct = default);

        /// <summary>
        /// Re-rank the full product catalogue in real-time against the simulator
        /// sliders. No AI call is made — scoring is pure C# and returns instantly.
        /// </summary>
        Task<SimulatorResponseDTO> SimulateAsync(
            SimulatorRequestDTO req,
            CancellationToken   ct = default);
    }
}
