using Stl.Fusion.EntityFramework;

namespace Samples.HelloCart.V2;

public class DbProductService : DbServiceBase<AppDbContext>, IProductService
{
    public DbProductService(IServiceProvider services) : base(services) { }

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
        var dbContext = CreateDbContext();
        await using var _ = dbContext.ConfigureAwait(false);

        var dbProduct = await dbContext.Products.FindAsync(ComposeKey(id), cancellationToken);
        if (dbProduct == null)
            return null;
        return new Product() { Id = dbProduct.Id, Price = dbProduct.Price };
    }
}
