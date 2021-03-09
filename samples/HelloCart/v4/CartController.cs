using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;

namespace Samples.HelloCart.V4
{
    [Route("api/[controller]")]
    [ApiController, JsonifyErrors]
    public class CartController : ControllerBase, ICartService
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService) => _cartService = cartService;

        // Commands

        [HttpPost("edit")]
        public Task EditAsync([FromBody] EditCommand<Cart> command, CancellationToken cancellationToken = default)
            => _cartService.EditAsync(command, cancellationToken);

        // Queries

        [HttpGet("find"), Publish]
        public Task<Cart?> FindAsync(string id, CancellationToken cancellationToken = default)
            => _cartService.FindAsync(id, cancellationToken);

        [HttpGet("getTotal"), Publish]
        public Task<decimal> GetTotalAsync(string id, CancellationToken cancellationToken = default)
            => _cartService.GetTotalAsync(id, cancellationToken);
    }
}
