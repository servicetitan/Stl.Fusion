using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services
{
    [Route("api/[controller]/[action]")]
    [ApiController, JsonifyErrors]
    public class ScreenshotController : ControllerBase
    {
        public static int CallCount { get; set; }
        protected IScreenshotService Service { get; }

        public ScreenshotController(IScreenshotService service) => Service = service;

        [HttpGet, Publish]
        public Task<Screenshot> GetScreenshot(int width)
        {
            CallCount++;
            return Service.GetScreenshot(width, HttpContext.RequestAborted);
        }
    }
}
