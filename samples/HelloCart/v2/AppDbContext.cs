using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Operations;

namespace Samples.HelloCart.V2;

public class DbProduct
{
    [Key] public string Id { get; set; } = "";
    public decimal Price { get; set; }
}

public class DbCart
{
    [Key] public string Id { get; set; } = "";
    public List<DbCartItem> Items { get; set; } = new();
}

public class DbCartItem
{
    public string DbCartId { get; set; } = "";
    public string DbProductId { get; set; } = "";
    public decimal Quantity { get; set; }
}

public class AppDbContext(DbContextOptions options) : DbContextBase(options)
{
    public DbSet<DbProduct> Products { get; protected set; } = null!;
    public DbSet<DbCart> Carts { get; protected set; } = null!;
    public DbSet<DbCartItem> CartItems { get; protected set; } = null!;

    // Stl.Fusion.EntityFramework tables
    public DbSet<DbOperation> Operations { get; protected set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var cart = modelBuilder.Entity<DbCart>();
        cart.HasMany(e => e.Items).WithOne();

        var cartItem = modelBuilder.Entity<DbCartItem>();
        cartItem.HasKey(e => new { e.DbCartId, e.DbProductId });
    }
}
