using Entities;
using Repositories;

namespace TestProject
{
    public class ProductRepositoryIntegrationTests : IDisposable
    {
        private readonly DatabaseFixture _fixture;
        private readonly ApiShopContext _dbContext;
        private readonly ProductsRepository _productsRepository;

        public ProductRepositoryIntegrationTests()
        {
            _fixture = new DatabaseFixture();
            _dbContext = _fixture.Context;
            _productsRepository = new ProductsRepository(_dbContext);
        }
        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        
        public async Task GetProducts_ShouldFilterSortAndPaginate_WhenParametersAreProvided()
        {
            
            _dbContext.OrderItems.RemoveRange(_dbContext.OrderItems);
            _dbContext.Products.RemoveRange(_dbContext.Products);
            _dbContext.Categories.RemoveRange(_dbContext.Categories);
            await _dbContext.SaveChangesAsync();

            
            var catElec = new Category { CategoryName = "Electronics" };
            var catHome = new Category { CategoryName = "Home" };
            await _dbContext.Categories.AddRangeAsync(catElec, catHome);
            await _dbContext.SaveChangesAsync();

            
            var prod1 = new Product { ProductName = "Cheap Radio", Price = 50, Description = "Small radio", CategoryId = catElec.CategoryId };
            var prod2 = new Product { ProductName = "Expensive TV", Price = 2000, Description = "Big screen TV", CategoryId = catElec.CategoryId };
            var prod3 = new Product { ProductName = "Gaming Mouse", Price = 100, Description = "Optical mouse", CategoryId = catElec.CategoryId };
            var prod4 = new Product { ProductName = "Sofa", Price = 1500, Description = "Comfy sofa", CategoryId = catHome.CategoryId }; // קטגוריה אחרת

            await _dbContext.Products.AddRangeAsync(prod1, prod2, prod3, prod4);
            await _dbContext.SaveChangesAsync();

            _dbContext.ChangeTracker.Clear();

            
            int?[] catIds = new int?[] { catElec.CategoryId };

            var result = await _productsRepository.GetProducts(
                position: 1,
                skip: 10,
                categoryIds: catIds,
                description: null,
                maxPrice: 3000,
                minPrice: 60
            );

            
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);

            
            Assert.Equal("Gaming Mouse", result.Items[0].ProductName);
            Assert.Equal("Expensive TV", result.Items[1].ProductName);

            
            Assert.NotNull(result.Items[0].Category);
            Assert.Equal("Electronics", result.Items[0].Category.CategoryName);
        }
        [Fact]
        public async Task GetProducts_ShouldReturnEmptyList_WhenDescriptionDoesNotMatch()
        {
            
            _dbContext.OrderItems.RemoveRange(_dbContext.OrderItems);
            _dbContext.Products.RemoveRange(_dbContext.Products);
            _dbContext.Categories.RemoveRange(_dbContext.Categories);
            await _dbContext.SaveChangesAsync();

            
            var category = new Category { CategoryName = "Food" };
            await _dbContext.Categories.AddAsync(category);
            await _dbContext.SaveChangesAsync();

            var prod = new Product
            {
                ProductName = "Pizza",
                Price = 50,
                Description = "Tasty cheese pizza",
                CategoryId = category.CategoryId
            };
            await _dbContext.Products.AddAsync(prod);
            await _dbContext.SaveChangesAsync();

            _dbContext.ChangeTracker.Clear();

            
            var result = await _productsRepository.GetProducts(
                position: 1,
                skip: 10,
                categoryIds: new int?[0],
                description: "Burger", 
                maxPrice: null,
                minPrice: null
            );

            
            Assert.NotNull(result.Items);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

    }
}