#if NETCOREAPP
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Http;
using ControllerBase = System.Web.Http.ApiController;
#endif
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services;

[JsonifyErrors, UseDefaultSession]
public class TimeController : ControllerBase, ITimeService
{
    protected ITimeService Service { get; }

    public TimeController(ITimeService service) => Service = service;

    [HttpGet, Publish]
    public Task<DateTime> GetTime(CancellationToken cancellationToken = default) 
        => Service.GetTime(cancellationToken);

    [HttpGet, Publish]
    public Task<DateTime> GetTimeNoControllerMethod(CancellationToken cancellationToken = default)
        => Service.GetTimeNoControllerMethod(cancellationToken);

    [HttpGet]
    public Task<DateTime> GetTimeNoPublication(CancellationToken cancellationToken = default)
        => Service.GetTimeNoPublication(cancellationToken);

    [HttpGet, Publish]
    public Task<DateTime> GetTimeWithDelay(CancellationToken cancellationToken = default)
        => Service.GetTimeWithDelay(cancellationToken);

    [HttpGet, Publish]
    public Task<string?> GetFormattedTime(string format, CancellationToken cancellationToken = default)
        => Service.GetFormattedTime(format ?? "", cancellationToken);

    [HttpGet, Publish]
    public Task<DateTime> GetTimeWithOffset(TimeSpan offset)
        => Service.GetTimeWithOffset(offset);
}
