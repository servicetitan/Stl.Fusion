using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;

namespace Samples.HelloCart.V4
{
    [Route("api/[controller]/[action]")]
    [ApiController, JsonifyErrors]
    public class CartController : ControllerBase, ICartService
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
            => _cartService = cartService;

        // Commands

        [HttpPost]
        public Task Edit([FromBody] EditCommand<Cart> command, CancellationToken cancellationToken = default)
            => _cartService.Edit(command, cancellationToken);

        // Queries

        [HttpGet, Publish]
        public Task<Cart?> TryGet(string id, CancellationToken cancellationToken = default)
            => _cartService.TryGet(id, cancellationToken);

        [HttpGet, Publish]
        public Task<decimal> GetTotal(string id, CancellationToken cancellationToken = default)
            => _cartService.GetTotal(id, cancellationToken);
    }
}
