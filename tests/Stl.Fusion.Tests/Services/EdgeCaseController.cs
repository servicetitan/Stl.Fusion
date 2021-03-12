using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services
{
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
}
