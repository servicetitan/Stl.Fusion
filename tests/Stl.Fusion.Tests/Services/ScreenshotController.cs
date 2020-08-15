using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ScreenshotController : FusionController
    {
        protected IScreenshotService Service { get; }

        public ScreenshotController(IPublisher publisher, IScreenshotService service) : base(publisher)
            => Service = service;

        [HttpGet]
        public Task<Screenshot> GetScreenshotAsync(int width)
            => PublishAsync(ct => Service.GetScreenshotAsync(width, ct));
    }
}
