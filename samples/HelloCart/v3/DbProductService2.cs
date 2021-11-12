using Samples.HelloCart.V2;
using Stl.Fusion.EntityFramework;

namespace Samples.HelloCart.V3;

public class DbProductService2 : DbServiceBase<AppDbContext>, IProductService
{
    private readonly IDbEntityResolver<string, DbProduct> _productResolver;

    public DbProductService2(
        IServiceProvider services,
        IDbEntityResolver<string, DbProduct> productResolver)
        : base(services)
        => _productResolver = productResolver;

    public virtual async Task Edit(EditCommand<Product> command, CancellationToken cancellationToken = default)
    {
        var (productId, product) = command;
        if (string.IsNullOrEmpty(productId))
            throw new ArgumentOutOfRangeException(nameof(command));
        if (Computed.IsInvalidating()) {
            _ = Get(productId, default);
            return;
        }

        await using var dbContext = await CreateCommandDbContext(cancellationToken);
        var dbProduct = await dbContext.Products.FindAsync(ComposeKey(productId), cancellationToken);
        if (product == null) {
            if (dbProduct != null)
                dbContext.Remove(dbProduct);
        }
        else {
            if (dbProduct != null)
                dbProduct.Price = product.Price;
            else
                dbContext.Add(new DbProduct { Id = productId, Price = product.Price });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<Product?> Get(string id, CancellationToken cancellationToken = default)
    {
        var dbProduct = await _productResolver.Get(id, cancellationToken);
        return dbProduct == null ? null : new Product() {
            Id = dbProduct.Id,
            Price = dbProduct.Price
        };
    }
}
