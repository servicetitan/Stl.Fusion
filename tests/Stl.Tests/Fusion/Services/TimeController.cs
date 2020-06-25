using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;

namespace Stl.Tests.Fusion.Services
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeController : Controller
    {
        protected ITimeService TimeService { get; }
        protected IPublisher Publisher { get; }

        public TimeController(
            ITimeService timeService,
            IPublisher publisher)
        {
            TimeService = timeService;
            Publisher = publisher;
        }

        [HttpGet("get")]
        public async Task<ActionResult<DateTime>> GetTimeAsync()
        {
            var c = await HttpContext.TryPublishAsync(Publisher, _ => TimeService.GetTimeAsync());
            return c.Value;
        }
    }
}
