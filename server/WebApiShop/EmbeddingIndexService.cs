// EmbeddingIndexService.cs
// Runs once at .NET startup: fetches all products from the DB,
// sends them to Python /index so it can build the semantic embedding index.
using DTOs;
using Services;

public class EmbeddingIndexService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<EmbeddingIndexService> _logger;

    public EmbeddingIndexService(
        IServiceProvider services,
        IHttpClientFactory factory,
        ILogger<EmbeddingIndexService> logger)
    {
        _services = services;
        _factory  = factory;
        _logger   = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // IProductsServices is scoped — resolve from a fresh scope
            using var scope = _services.CreateScope();
            var productService = scope.ServiceProvider
                .GetRequiredService<IProductsServices>();

            int currentMonth = DateTime.Now.Month;

            var page = await productService.GetProducts(
                1, 200, Array.Empty<int?>(), null, null, null);

            // Cap at 50 — keeps embedding cost predictable for large catalogs.
            // Raise this limit (and the Python _MAX_INDEX_SIZE) if your catalog grows.
            var productList = (page.Data ?? new List<ProductDTO>())
                .Take(50)
                .Select(p =>
            {
                var monthConfig = p.MonthConfigs?
                    .FirstOrDefault(m => m.MonthNumber == currentMonth);

                bool available = monthConfig?.IsAvailable ?? true;
                decimal price  = (monthConfig?.SpecialPrice > 0
                    ? monthConfig.SpecialPrice
                    : p.Price);

                return new
                {
                    id          = p.ProductId,
                    name        = p.ProductName ?? string.Empty,
                    price       = price,
                    description = p.Description ?? string.Empty,
                    inStock     = available
                };
            }).ToList();

            var http = _factory.CreateClient();
            var res  = await http.PostAsJsonAsync(
                "http://localhost:8001/index", productList, cancellationToken);

            if (res.IsSuccessStatusCode)
            {
                var result = await res.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Embedding index built: {Result}", result);
            }
            else
            {
                _logger.LogWarning("Failed to build embedding index: {Status}", res.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Non-fatal — chat still works via the fallback product list
            _logger.LogWarning("Embedding index build failed: {Message}", ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
