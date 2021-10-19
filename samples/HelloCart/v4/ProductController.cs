using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;

namespace Samples.HelloCart.V4;

[Route("api/[controller]/[action]")]
[ApiController, JsonifyErrors]
public class ProductController : ControllerBase, IProductService
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
        => _productService = productService;

    // Commands

    [HttpPost]
    public Task Edit([FromBody] EditCommand<Product> command, CancellationToken cancellationToken = default)
        => _productService.Edit(command, cancellationToken);

    // Queries

    [HttpGet, Publish]
    public Task<Product?> TryGet(string id, CancellationToken cancellationToken = default)
        => _productService.TryGet(id, cancellationToken);
}
