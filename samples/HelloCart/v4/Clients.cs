using System.Threading;
using System.Threading.Tasks;
using RestEase;

namespace Samples.HelloCart.V4
{
    [BasePath("product")]
    public interface IProductClient
    {
        [Post("edit")]
        Task EditAsync([Body] EditCommand<Product> command, CancellationToken cancellationToken = default);
        [Get("find")]
        Task<Product?> FindAsync(string id, CancellationToken cancellationToken = default);
    }

    [BasePath("cart")]
    public interface ICartClient
    {
        [Post("edit")]
        Task EditAsync(EditCommand<Cart> command, CancellationToken cancellationToken = default);
        [Get("find")]
        Task<Cart?> FindAsync(string id, CancellationToken cancellationToken = default);
        [Get("getTotal")]
        Task<decimal> GetTotalAsync(string id, CancellationToken cancellationToken = default);
    }
}
