using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;

namespace Samples.HelloCart.V4;

[Route("api/[controller]/[action]")]
[ApiController, JsonifyErrors, UseDefaultSession]
public class CartController : ControllerBase, ICartService
{
    private readonly ICartService _cartService;
    private readonly ICommander _commander;

    public CartController(ICartService cartService, ICommander commander)
    {
        _cartService = cartService;
        _commander = commander;
    }

    // Commands

    [HttpPost]
    public Task Edit([FromBody] EditCommand<Cart> command, CancellationToken cancellationToken = default)
        => _commander.Call(command, cancellationToken);

    // Queries

    [HttpGet, Publish]
    public Task<Cart?> Get(string id, CancellationToken cancellationToken = default)
        => _cartService.Get(id, cancellationToken);

    [HttpGet, Publish]
    public Task<decimal> GetTotal(string id, CancellationToken cancellationToken = default)
        => _cartService.GetTotal(id, cancellationToken);
}
