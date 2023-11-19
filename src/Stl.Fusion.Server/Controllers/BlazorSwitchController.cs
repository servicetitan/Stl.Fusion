using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server.Endpoints;

namespace Stl.Fusion.Server.Controllers;

[Route("~/fusion/blazorMode")]
public sealed class BlazorModeController(BlazorModeEndpoint handler) : ControllerBase
{
    [HttpGet]
    [HttpGet("{isBlazorServer}")]
    public async Task<IActionResult> Invoke(string? isBlazorServer, string? redirectTo = null)
    {
        var result = await handler.Invoke(HttpContext, isBlazorServer, redirectTo).ConfigureAwait(false);
        return Redirect(result.Url);
    }
}
