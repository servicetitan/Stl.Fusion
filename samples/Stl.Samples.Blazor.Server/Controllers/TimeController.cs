using System;
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
    public class TimeController : Controller, ITimeService
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
        public async Task<DateTime> GetTimeAsync(CancellationToken cancellationToken)
        {
            var c = await HttpContext.TryPublishAsync(Publisher, 
                _ => TimeService.GetTimeAsync(cancellationToken),
                cancellationToken);
            return c.Value;
        }
    }
}
