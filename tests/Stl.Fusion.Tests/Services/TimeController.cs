using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TimeController : FusionController
    {
        protected ITimeService Service { get; }

        public TimeController(ITimeService service) => Service = service;

        [HttpGet]
        public Task<DateTime> GetTimeAsync()
            => PublishAsync(ct => Service.GetTimeAsync(ct));

        [HttpGet]
        public Task<string?> GetFormattedTimeAsync(string? format)
        {
            format ??= "";
            return PublishAsync(ct => Service.GetFormattedTimeAsync(format, ct));
        }
    }
}
