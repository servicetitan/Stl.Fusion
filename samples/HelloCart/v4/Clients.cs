using System.Threading;
using System.Threading.Tasks;
using RestEase;

namespace Samples.HelloCart.V4
{
    [BasePath("product")]
    public interface IProductClient
    {
        [Post("edit")]
        Task Edit([Body] EditCommand<Product> command, CancellationToken cancellationToken);
        [Get("tryGet")]
        Task<Product?> TryGet(string id, CancellationToken cancellationToken);
    }

    [BasePath("cart")]
    public interface ICartClient
    {
        [Post("edit")]
        Task Edit([Body] EditCommand<Cart> command, CancellationToken cancellationToken);
        [Get("tryGet")]
        Task<Cart?> TryGet(string id, CancellationToken cancellationToken);
        [Get("getTotal")]
        Task<decimal> GetTotal(string id, CancellationToken cancellationToken);
    }
}
