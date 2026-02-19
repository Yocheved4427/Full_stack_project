using Microsoft.EntityFrameworkCore;
using Repositories;
using System;

namespace TestProject
{
    public class DatabaseFixture : IDisposable
    {
        public ApiShopContext Context { get; private set; }

        
        public DatabaseFixture()
        {
            var options = new DbContextOptionsBuilder<ApiShopContext>()

                .UseSqlServer("Data Source=Yocheved;Initial Catalog=ApiShopTest;Integrated Security=True;Pooling=False;TrustServerCertificate=True")
                .Options;
            
            Context = new ApiShopContext(options);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Database.EnsureDeleted();
            Context.Dispose();
        }
    }
}