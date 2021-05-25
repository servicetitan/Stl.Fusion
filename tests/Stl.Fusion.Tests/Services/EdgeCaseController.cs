using System.Threading;
using System.Threading.Tasks;
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
    public class EdgeCaseController : ControllerBase, IEdgeCaseService
    {
        protected IEdgeCaseService Service { get; }

        public EdgeCaseController(IEdgeCaseService service) => Service = service;

        [HttpGet]
        public Task<string> GetSuffix(CancellationToken cancellationToken)
            => Service.GetSuffix(cancellationToken);

        [HttpPost]
        public Task SetSuffix([FromQuery] string? suffix, CancellationToken cancellationToken)
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

    [JsonifyErrors(RewriteErrors = true, Order = 1)]
    public class EdgeCaseRewriteController : EdgeCaseController
    {
        public EdgeCaseRewriteController(IEdgeCaseService service) : base(service) { }
    }

    #else
    
    [JsonifyErrors]
    public class EdgeCaseController : ApiController, IEdgeCaseService
    {
        protected IEdgeCaseService Service { get; }

        public EdgeCaseController(IEdgeCaseService service) => Service = service;

        [HttpGet]
        public Task<string> GetSuffix(CancellationToken cancellationToken)
            => Service.GetSuffix(cancellationToken);

        [HttpPost]
        public Task SetSuffix(string? suffix, CancellationToken cancellationToken)
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

    //[JsonifyErrors(RewriteErrors = true, Order = 1)]
    // It seems there is no concept of filter ordering. Let see what we will get.
    [JsonifyErrors(RewriteErrors = true)]
    public class EdgeCaseRewriteController : EdgeCaseController
    {
        public EdgeCaseRewriteController(IEdgeCaseService service) : base(service) { }
    }
    
    #endif
}
