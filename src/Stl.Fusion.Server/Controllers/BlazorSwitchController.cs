using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server.Endpoints;

namespace Stl.Fusion.Server.Controllers;

[Route("~/fusion/blazorMode")]
public sealed class BlazorSwitchController : ControllerBase
{
    private readonly BlazorSwitchEndpoint _handler;

    public BlazorSwitchController(BlazorSwitchEndpoint handler)
        => _handler = handler;

    [HttpGet("{isServerSideBlazor}")]
    public async Task<IActionResult> Invoke(bool isServerSideBlazor, string? redirectTo = null)
    {
        var result = (BlazorSwitchEndpoint.RedirectResult)await _handler.Invoke(
            HttpContext, isServerSideBlazor, redirectTo);
        return Redirect(result.Url);
    }
}
