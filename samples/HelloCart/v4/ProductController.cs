using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;

namespace Samples.HelloCart.V4;

[Route("api/[controller]/[action]")]
[ApiController, JsonifyErrors, UseDefaultSession]
public sealed class ProductController : ControllerBase, IProductService
{
    private readonly IProductService _productService;
    private readonly ICommander _commander;

    public ProductController(IProductService productService, ICommander commander)
    {
        _productService = productService;
        _commander = commander;
    }

    // Commands

    [HttpPost]
    public Task Edit([FromBody] EditCommand<Product> command, CancellationToken cancellationToken = default)
        => _commander.Call(command, cancellationToken);

    // Queries

    [HttpGet, Publish]
    public Task<Product?> Get(string id, CancellationToken cancellationToken = default)
        => _productService.Get(id, cancellationToken);
}
