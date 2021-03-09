using System.Threading;
using System.Threading.Tasks;
using RestEase;

namespace Samples.HelloCart.V4
{
    [BasePath("product")]
    public interface IProductClient
    {
        [Post("edit")]
        Task EditAsync([Body] EditCommand<Product> command, CancellationToken cancellationToken);
        [Get("find")]
        Task<Product?> FindAsync(string id, CancellationToken cancellationToken);
    }

    [BasePath("cart")]
    public interface ICartClient
    {
        [Post("edit")]
        Task EditAsync([Body] EditCommand<Cart> command, CancellationToken cancellationToken);
        [Get("find")]
        Task<Cart?> FindAsync(string id, CancellationToken cancellationToken);
        [Get("getTotal")]
        Task<decimal> GetTotalAsync(string id, CancellationToken cancellationToken);
    }
}
