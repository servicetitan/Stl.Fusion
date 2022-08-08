#if NETCOREAPP
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Http;
using ControllerBase = System.Web.Http.ApiController;
#endif
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services;

[JsonifyErrors, UseDefaultSession]
public class EdgeCaseController : ControllerBase, IEdgeCaseService
{
    protected IEdgeCaseService Service { get; }

    public EdgeCaseController(IEdgeCaseService service) => Service = service;

    [HttpGet]
    public Task<string> GetSuffix(CancellationToken cancellationToken = default)
        => Service.GetSuffix(cancellationToken);

    [HttpPost]
#if NETCOREAPP
    public Task SetSuffix([FromQuery] string? suffix, CancellationToken cancellationToken = default)
#else
    // TODO: add tests for RestEase calls with different options FromQuery, FromBody, from path segment;
    public Task SetSuffix(string? suffix, CancellationToken cancellationToken = default)
#endif
        => Service.SetSuffix(suffix ?? "", cancellationToken);

    [HttpGet]
    public Task<long?> GetNullable(long source, CancellationToken cancellationToken = default) 
        => Service.GetNullable(source, cancellationToken);

    [HttpGet, Publish]
    public Task<string> ThrowIfContainsError(string? source, CancellationToken cancellationToken = default)
        => Service.ThrowIfContainsError(source ?? "", cancellationToken);

    [HttpGet, Publish]
    public Task<string> ThrowIfContainsErrorNonCompute(string? source, CancellationToken cancellationToken = default)
        => Service.ThrowIfContainsErrorNonCompute(source ?? "", cancellationToken);
}

#if NETCOREAPP
[JsonifyErrors(Order = 1), UseDefaultSession]
#else
// It seems there is no concept of filter ordering. Let see what we will get.
[JsonifyErrors]
#endif
public class EdgeCaseRewriteController : EdgeCaseController
{
    public EdgeCaseRewriteController(IEdgeCaseService service) : base(service) { }
}
