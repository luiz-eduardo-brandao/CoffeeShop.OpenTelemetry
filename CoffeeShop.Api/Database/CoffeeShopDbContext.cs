using CoffeeShop.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShop.Api.Database
{
    public class CoffeeShopDbContext : DbContext
    {
        public CoffeeShopDbContext(DbContextOptions<CoffeeShopDbContext> options) 
            : base(options)
        {
        }

        public DbSet<Sale> Sales { get; set; }
    }
}
