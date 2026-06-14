// ChatController.cs
using Microsoft.AspNetCore.Mvc;
using Services;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly HttpClient _http;

    public ChatController(IHttpClientFactory factory)
        => _http = factory.CreateClient();

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest req)
    {
        // Python uses the semantic index built at startup — just forward message + history
        var payload = new
        {
            message  = req.Message,
            history  = req.History,
            products = Array.Empty<object>()
        };

        var res = await _http.PostAsJsonAsync(
            "http://localhost:8001/chat", payload);

        if (!res.IsSuccessStatusCode)
            return StatusCode(500, "AI service unavailable");

        var data = await res.Content.ReadFromJsonAsync<ChatResponse>();
        return Ok(data);
    }
}

public record ChatRequest(
    string Message,
    List<HistoryItem> History,
    List<object> Products);

public record HistoryItem(string Role, string Content);
public record ChatResponse(string Reply, string? SuggestedSearch);

