using RestEase;

namespace Samples.HelloCart.V4;

[BasePath("product")]
public interface IProductClientDef
{
    [Post("edit")]
    Task Edit([Body] EditCommand<Product> command, CancellationToken cancellationToken);
    [Get("get")]
    Task<Product?> Get(string id, CancellationToken cancellationToken);
}

[BasePath("cart")]
public interface ICartClientDef
{
    [Post("edit")]
    Task Edit([Body] EditCommand<Cart> command, CancellationToken cancellationToken);
    [Get("get")]
    Task<Cart?> Get(string id, CancellationToken cancellationToken);
    [Get("getTotal")]
    Task<decimal> GetTotal(string id, CancellationToken cancellationToken);
}
