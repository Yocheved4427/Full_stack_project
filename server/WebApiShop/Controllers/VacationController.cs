// VacationController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Services;
using DTOs;

[ApiController]
[Route("api/[controller]")]
public class VacationController : ControllerBase
{
    private readonly IVacationService _vacationService;

    public VacationController(IVacationService vacationService)
        => _vacationService = vacationService;

    /// <summary>
    /// Build a personalised Dream Vacation profile and return matched packages.
    /// Accepts multipart/form-data with exactly one of: Text, Audio, or Image.
    /// </summary>
    /// <remarks>
    /// Text example:
    ///   POST /api/vacation/build   Content-Type: multipart/form-data
    ///   Form field: Text = "I want a calm vacation in August with a nice view, not expensive"
    ///
    /// Audio example (file upload):
    ///   Form file: Audio = &lt;recording.mp3&gt;
    ///
    /// Image example (file upload):
    ///   Form file: Image = &lt;beach.jpg&gt;
    /// </remarks>
    [HttpPost("build")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(VacationBuildResponseDTO), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(502)]
    public async Task<IActionResult> Build(
        [FromForm] string? text,
        IFormFile? audio,
        IFormFile? image,
        CancellationToken ct)
    {
        // ── Input validation ────────────────────────────────────────────────
        bool hasText  = !string.IsNullOrWhiteSpace(text);
        bool hasAudio = audio  is { Length: > 0 };
        bool hasImage = image  is { Length: > 0 };

        if (!hasText && !hasAudio && !hasImage)
            return BadRequest(
                "Provide exactly one input: a Text field, an Audio file, or an Image file.");

        int inputCount = (hasText ? 1 : 0) + (hasAudio ? 1 : 0) + (hasImage ? 1 : 0);
        if (inputCount > 1)
            return BadRequest(
                "Only one input type may be used per request (Text, Audio, or Image).");

        // ── Read uploaded file bytes (if any) ───────────────────────────────
        byte[]? audioBytes = null;
        string? audioFileName    = null;
        string? audioContentType = null;

        byte[]? imageBytes       = null;
        string? imageContentType = null;

        if (hasAudio)
        {
            audioFileName    = audio!.FileName;
            audioContentType = audio.ContentType;
            using var ms     = new MemoryStream();
            await audio.CopyToAsync(ms, ct);
            audioBytes = ms.ToArray();
        }

        if (hasImage)
        {
            imageContentType = image!.ContentType;
            using var ms     = new MemoryStream();
            await image.CopyToAsync(ms, ct);
            imageBytes = ms.ToArray();
        }

        // ── Delegate to service ─────────────────────────────────────────────
        try
        {
            var result = await _vacationService.BuildVacationAsync(
                text:             hasText ? text!.Trim() : null,
                audioBytes:       audioBytes,
                audioFileName:    audioFileName,
                audioContentType: audioContentType,
                imageBytes:       imageBytes,
                imageContentType: imageContentType,
                ct:               ct);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, $"AI service error: {ex.Message}");
        }
    }

    /// <summary>
    /// What-If Simulator: re-rank the full product catalogue in real-time based on
    /// four slider values. No AI call is made — returns instantly.
    /// </summary>
    [HttpPost("simulator")]
    [ProducesResponseType(typeof(SimulatorResponseDTO), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Simulate(
        [FromBody] SimulatorRequestDTO req,
        CancellationToken ct)
    {
        if (req.LuxuryLevel    is < 1 or > 5) return BadRequest("LuxuryLevel must be between 1 and 5.");
        if (req.NatureVibe     is < 1 or > 5) return BadRequest("NatureVibe must be between 1 and 5.");
        if (req.BudgetLimit    < 0)            return BadRequest("BudgetLimit must be 0 or greater.");
        if (req.AttractionCount < 0)           return BadRequest("AttractionCount must be 0 or greater.");

        try
        {
            var result = await _vacationService.SimulateAsync(req, ct);
            return Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
