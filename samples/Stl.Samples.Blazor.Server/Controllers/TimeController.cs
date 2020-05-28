using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeController : Controller
    {
        protected ITimeProvider TimeProvider { get; }
        protected IPublisher Publisher { get; }

        public TimeController(
            ITimeProvider timeProvider,
            IPublisher publisher)
        {
            TimeProvider = timeProvider;
            Publisher = publisher;
        }

        [HttpGet("get")]
        public async Task<ActionResult<DateTime>> GetTimeAsync()
        {
            var c = await HttpContext.TryPublishAsync(Publisher, _ => TimeProvider.GetTimeAsync());
            return c.Value;
        }
    }
}
