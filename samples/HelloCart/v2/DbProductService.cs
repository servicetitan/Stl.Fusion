using Stl.Fusion.EntityFramework;

namespace Samples.HelloCart.V2;

public class DbProductService : IProductService
{
    private readonly DbHub<AppDbContext> _dbHub;

    public DbProductService(DbHub<AppDbContext> dbHub)
        => _dbHub = dbHub;

    public virtual async Task Edit(EditCommand<Product> command, CancellationToken cancellationToken = default)
    {
        var (productId, product) = command;
        if (string.IsNullOrEmpty(productId))
            throw new ArgumentOutOfRangeException(nameof(command));
        if (Computed.IsInvalidating()) {
            _ = Get(productId, default);
            return;
        }

        await using var dbContext = await _dbHub.CreateCommandDbContext(cancellationToken);
        var dbProduct = await dbContext.Products.FindAsync(DbKey.Compose(productId), cancellationToken);
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
        await using var dbContext = _dbHub.CreateDbContext();
        var dbProduct = await dbContext.Products.FindAsync(DbKey.Compose(id), cancellationToken);
        if (dbProduct == null)
            return null;
        return new Product() { Id = dbProduct.Id, Price = dbProduct.Price };
    }
}
