using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;

namespace Samples.HelloCart.V4;

[Route("api/[controller]/[action]")]
[ApiController, JsonifyErrors, UseDefaultSession]
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
    public Task<Cart?> Get(string id, CancellationToken cancellationToken = default)
        => _cartService.Get(id, cancellationToken);

    [HttpGet, Publish]
    public Task<decimal> GetTotal(string id, CancellationToken cancellationToken = default)
        => _cartService.GetTotal(id, cancellationToken);
}
