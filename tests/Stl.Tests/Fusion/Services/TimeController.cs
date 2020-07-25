using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;

namespace Stl.Tests.Fusion.Services
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeController : FusionController
    {
        protected ITimeService TimeService { get; }

        public TimeController(IPublisher publisher, ITimeService timeService)
            : base(publisher)
            => TimeService = timeService;

        [HttpGet("get")]
        public Task<DateTime> GetTimeAsync()
        {
            return PublishAsync(_ => TimeService.GetTimeAsync());
        }
    }
}
