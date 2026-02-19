using Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Repositories;
namespace TestProject
{
    public class CategoryRepositoryUnitTesting
    {
        [Fact]
        public async Task GetCategories_ShouldReturnList_WhenContextHasData()
        {
            var options = new DbContextOptions<ApiShopContext>();

            var mockContext = new Mock<ApiShopContext>(options);

            var categoriesList = new List<Category>
    {
        new Category { CategoryId = 1, CategoryName = "Toys" },
        new Category { CategoryId = 2, CategoryName = "Books" }
    };

            
            mockContext.Setup(x => x.Categories).ReturnsDbSet(categoriesList);
            var repository = new CategoriesRepository(mockContext.Object);
            var result = await repository.GetCategories();
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("Toys", result.First().CategoryName);
        }
        [Fact]
        public async Task GetCategories_ShouldReturnEmptyList_WhenContextIsEmpty()
        {
             var options = new DbContextOptions<ApiShopContext>();
            var mockContext = new Mock<ApiShopContext>(options);
            mockContext.Setup(x => x.Categories).ReturnsDbSet(new List<Category>());
            var repository = new CategoriesRepository(mockContext.Object);
            var result = await repository.GetCategories();
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
