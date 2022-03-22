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
    public static int CallCount { get; set; }
    protected IScreenshotService Service { get; }

    public ScreenshotController(IScreenshotService service) => Service = service;

    [HttpGet, Publish]
    public Task<Screenshot> GetScreenshot(int width)
    {
        CallCount++;
        return Service.GetScreenshot(width, this.RequestAborted());
    }
}
