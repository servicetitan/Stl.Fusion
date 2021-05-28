using System.Threading;
using System.Threading.Tasks;
#if NETCOREAPP
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Http;
using ControllerBase = System.Web.Http.ApiController;
#endif
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services
{
    [JsonifyErrors]
    public class EdgeCaseController : ControllerBase, IEdgeCaseService
    {
        protected IEdgeCaseService Service { get; }

        public EdgeCaseController(IEdgeCaseService service) => Service = service;

        [HttpGet]
        public Task<string> GetSuffix(CancellationToken cancellationToken)
            => Service.GetSuffix(cancellationToken);

        [HttpPost]
#if NETCOREAPP
        public Task SetSuffix([FromQuery] string? suffix, CancellationToken cancellationToken)
#else
        // TODO: add tests for RestEase calls with different options FromQuery, FromBody, from path segment;
        public Task SetSuffix(string? suffix, CancellationToken cancellationToken)
#endif        
            => Service.SetSuffix(suffix ?? "", cancellationToken);

        [HttpGet, Publish]
        public Task<string> ThrowIfContainsError(string? source, CancellationToken cancellationToken)
            => Service.ThrowIfContainsError(source ?? "", cancellationToken);

        [HttpGet, Publish]
        public Task<string> ThrowIfContainsErrorRewriteErrors(string? source, CancellationToken cancellationToken)
            => Service.ThrowIfContainsErrorRewriteErrors(source ?? "", cancellationToken);

        [HttpGet, Publish]
        public Task<string> ThrowIfContainsErrorNonCompute(string? source, CancellationToken cancellationToken)
            => Service.ThrowIfContainsErrorNonCompute(source ?? "", cancellationToken);
    }
    
    #if NETCOREAPP
    [JsonifyErrors(RewriteErrors = true, Order = 1)]
    #else
    // It seems there is no concept of filter ordering. Let see what we will get.
    [JsonifyErrors(RewriteErrors = true)]
    #endif
    public class EdgeCaseRewriteController : EdgeCaseController
    {
        public EdgeCaseRewriteController(IEdgeCaseService service) : base(service) { }
    }
}
