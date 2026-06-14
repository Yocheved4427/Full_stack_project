// SearchController.cs
using Microsoft.AspNetCore.Mvc;
using Services;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly HttpClient _http;
    private readonly IProductsServices _productService;

    public SearchController(IHttpClientFactory factory, IProductsServices productService)
    {
        _http           = factory.CreateClient();
        _productService = productService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SearchQuery req)
    {
        int currentMonth = DateTime.Now.Month;

        // Fetch active products (Python uses its pre-built index for speed;
        // these are sent as fallback in case the index isn't ready yet)
        var page = await _productService.GetProducts(1, 200,
            Array.Empty<int?>(), null, null, null);

        var productList = page.Data?.Select(p =>
        {
            var monthConfig = p.MonthConfigs?
                .FirstOrDefault(m => m.MonthNumber == currentMonth);

            decimal price = monthConfig?.SpecialPrice > 0
                ? monthConfig.SpecialPrice
                : p.Price;

            return new
            {
                name        = p.ProductName ?? string.Empty,
                price       = price,
                description = p.Description ?? string.Empty,
                inStock     = true
            };
        }).ToList() ?? new();

        var res = await _http.PostAsJsonAsync(
            "http://localhost:8001/search",
            new { query = req.Query, products = productList, top_k = req.TopK });

        if (!res.IsSuccessStatusCode)
            return StatusCode(500, "Search service unavailable");

        var data = await res.Content.ReadFromJsonAsync<SearchResponse>();
        return Ok(data);
    }
}

public record SearchQuery(string Query, int TopK = 5);
public record SearchResponse(List<object> Results);
