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
    public class ScreenshotController : FusionController, IScreenshotService
    {
        private readonly IScreenshotService _screenshots;

        public ScreenshotController(IScreenshotService screenshots, IPublisher publisher)
            : base(publisher) 
            => _screenshots = screenshots;

        [HttpGet("get")]
        public async Task<Screenshot> GetScreenshotAsync(int width, CancellationToken cancellationToken)
        {
            var c = await HttpContext.TryPublishAsync(Publisher, 
                _ => _screenshots.GetScreenshotAsync(width, cancellationToken),
                cancellationToken);
            return c.Value;
        }
    }
}
