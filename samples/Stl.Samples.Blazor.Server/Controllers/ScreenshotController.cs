using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScreenshotController : Controller, IScreenshotService
    {
        protected IScreenshotService ScreenshotService { get; }
        protected IPublisher Publisher { get; }

        public ScreenshotController(
            IScreenshotService screenshotService,
            IPublisher publisher)
        {
            ScreenshotService = screenshotService;
            Publisher = publisher;
        }

        [HttpGet("get")]
        public async Task<Screenshot> GetScreenshotAsync(int width, CancellationToken cancellationToken)
        {
            var c = await HttpContext.TryPublishAsync(Publisher, 
                _ => ScreenshotService.GetScreenshotAsync(width, cancellationToken),
                cancellationToken);
            return c.Value;
        }
    }
}
