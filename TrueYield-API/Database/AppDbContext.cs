using Microsoft.EntityFrameworkCore;
using TrueYield_API.Features.Portfolio;

namespace TrueYield_API.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Position> Positions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Ensure decimal precision for financial apps
        modelBuilder.Entity<Position>()
            .Property(p => p.Quantity)
            .HasColumnType("numeric(18,8)");

        modelBuilder.Entity<Position>()
            .Property(p => p.UnitPurchasePrice)
            .HasColumnType("numeric(18,4)");

        modelBuilder.Entity<Position>()
            .Property(p => p.PurchaseExchangeRate)
            .HasColumnType("numeric(18,4)");
    }
}
