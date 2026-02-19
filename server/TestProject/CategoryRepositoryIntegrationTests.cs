using Entities;
using Repositories;
namespace TestProject
{
    public class CategoryRepositoryIntegrationTests : IDisposable
    {
        private readonly DatabaseFixture _fixture;
        private readonly ApiShopContext _dbContext;
        private readonly CategoriesRepository _categoryRepository;

        public CategoryRepositoryIntegrationTests()
        {
            _fixture = new DatabaseFixture();
            _dbContext = _fixture.Context;
            _categoryRepository = new CategoriesRepository(_dbContext);
        }
        public void Dispose()
        {
            _fixture.Dispose();
        }
        [Fact]
        public async Task GetCategories_ShouldReturnAllCategories_WhenCategoriesExist()
        {
            var products = _dbContext.Set<Product>();
            if (products.Any())
            {
                _dbContext.RemoveRange(products);
            }

            _dbContext.Categories.RemoveRange(_dbContext.Categories);
            await _dbContext.SaveChangesAsync();

            var cat1 = new Category { CategoryName = "Electronics" };
            var cat2 = new Category { CategoryName = "Clothing" };

            await _dbContext.Categories.AddRangeAsync(cat1, cat2);
            await _dbContext.SaveChangesAsync();

            _dbContext.ChangeTracker.Clear();
            var result = await _categoryRepository.GetCategories();
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.CategoryName == "Electronics");
        }

        [Fact]
        public async Task GetCategories_ShouldReturnEmptyList_WhenNoCategoriesExist()
        {
            _dbContext.Categories.RemoveRange(_dbContext.Categories);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
            var result = await _categoryRepository.GetCategories();
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}