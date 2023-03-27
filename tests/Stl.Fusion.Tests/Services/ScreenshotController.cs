#if NETCOREAPP
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Http;
using ControllerBase = System.Web.Http.ApiController;
#endif
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services;

[JsonifyErrors, UseDefaultSession]
public class ScreenshotController : ControllerBase
{
    protected IScreenshotService Service { get; }

    public static int CallCount { get; set; }

    public ScreenshotController(IScreenshotService service)
        => Service = service;

    [HttpGet, Publish]
    public Task<Screenshot> GetScreenshotAlt(int width, CancellationToken cancellationToken)
        => Service.GetScreenshot(width, cancellationToken);

    [HttpGet, Publish]
    public Task<Screenshot> GetScreenshot(int width, CancellationToken cancellationToken)
    {
        CallCount++;
        var screenshot = Service.GetScreenshot(width, cancellationToken);
        return screenshot;
    }
}
