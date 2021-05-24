using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
#if NETCOREAPP
using Microsoft.AspNetCore.Mvc;
#else
using System.Web;
using System.Web.Http;
#endif

using Stl.Fusion.Server;


namespace Stl.Fusion.Tests.Services
{
    #if NETCOREAPP

    [Route("api/[controller]/[action]")]
    [ApiController, JsonifyErrors]
    public class TimeController : ControllerBase
    {
        protected ITimeService Service { get; }

        public TimeController(ITimeService service) => Service = service;

        [HttpGet, Publish]
        public Task<DateTime> GetTime()
            => Service.GetTime(HttpContext.RequestAborted);

        [HttpGet, Publish]
        public Task<string?> GetFormattedTime(string? format)
            => Service.GetFormattedTime(format ?? "", HttpContext.RequestAborted);
    }

    #else

    // use default route template
    [JsonifyErrors]
    public class TimeController : ApiController
    {
        protected ITimeService Service { get; }

        public TimeController(ITimeService service) => Service = service;

        [HttpGet, Publish]
        public Task<DateTime> GetTime()
            => Service.GetTime(ActionContext.RequestAborted());

        [HttpGet, Publish]
        public Task<string?> GetFormattedTime(string? format)
            => Service.GetFormattedTime(format ?? "", ActionContext.RequestAborted());
    }

    #endif
}
