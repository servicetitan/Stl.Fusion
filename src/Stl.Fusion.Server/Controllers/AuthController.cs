using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server.Endpoints;

namespace Stl.Fusion.Server.Controllers;

public sealed class AuthController : Controller
{
    private readonly AuthEndpoints _handler;

    public AuthController(AuthEndpoints handler)
        => _handler = handler;

    [HttpGet("~/signIn")]
    [HttpGet("~/signIn/{scheme}")]
    public async Task<IActionResult> SignIn(string? scheme = null, string? returnUrl = null)
    {
        var result = await _handler.SignIn(HttpContext, scheme, returnUrl);
        return Challenge(result.AuthenticationProperties, result.AuthenticationSchemes);
    }

    [HttpGet("~/signOut")]
    public async Task<IActionResult> SignOut(string? returnUrl = null)
    {
        var result = await _handler.SignOut(HttpContext, returnUrl);
        return SignOut(result.AuthenticationProperties, result.AuthenticationSchemes);
    }
}
