using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;

namespace Samples.HelloCart.V4
{
    [Route("api/[controller]")]
    [ApiController, JsonifyErrors]
    public class ProductController : ControllerBase, IProductService
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService) => _productService = productService;

        // Commands

        [HttpPost("edit")]
        public Task EditAsync([FromBody] EditCommand<Product> command, CancellationToken cancellationToken = default)
            => _productService.EditAsync(command, cancellationToken);

        // Queries

        [HttpGet("find"), Publish]
        public Task<Product?> FindAsync(string id, CancellationToken cancellationToken = default)
            => _productService.FindAsync(id, cancellationToken);
    }
}
