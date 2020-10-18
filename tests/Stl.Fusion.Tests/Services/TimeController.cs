using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services
{
    [Route("api/[controller]/[action]")]
    [ApiController, JsonifyErrors]
    public class TimeController : ControllerBase
    {
        protected ITimeService Service { get; }

        public TimeController(ITimeService service) => Service = service;

        [HttpGet, Publish]
        public Task<DateTime> GetTimeAsync()
            => Service.GetTimeAsync(HttpContext.RequestAborted);

        [HttpGet, Publish]
        public Task<string?> GetFormattedTimeAsync(string? format)
            => Service.GetFormattedTimeAsync(format ?? "", HttpContext.RequestAborted);
    }
}
