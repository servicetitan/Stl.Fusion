#if NETCOREAPP
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Http;
using ControllerBase = System.Web.Http.ApiController;
#endif
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services;

[JsonifyErrors, UseDefaultSession]
public class TimeController : ControllerBase
{
    protected ITimeService Service { get; }

    public TimeController(ITimeService service) => Service = service;

    [HttpGet, Publish]
    public Task<DateTime> GetTime()
        => Service.GetTime(this.RequestAborted());

    [HttpGet, Publish]
    public Task<string?> GetFormattedTime(string? format)
        => Service.GetFormattedTime(format ?? "", this.RequestAborted());
}
