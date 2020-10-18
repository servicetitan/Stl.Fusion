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
        public Task<string> GetSuffixAsync(CancellationToken cancellationToken)
            => Service.GetSuffixAsync(cancellationToken);

        [HttpPost]
        public Task SetSuffixAsync([FromQuery] string? suffix, CancellationToken cancellationToken)
            => Service.SetSuffixAsync(suffix ?? "", cancellationToken);

        [HttpGet, Publish]
        public Task<string> ThrowIfContainsErrorAsync(string? source, CancellationToken cancellationToken)
            => Service.ThrowIfContainsErrorAsync(source ?? "", cancellationToken);

        [HttpGet, Publish]
        public Task<string> ThrowIfContainsErrorRewriteErrorsAsync(string? source, CancellationToken cancellationToken)
            => Service.ThrowIfContainsErrorRewriteErrorsAsync(source ?? "", cancellationToken);

        [HttpGet, Publish]
        public Task<string> ThrowIfContainsErrorNonComputeAsync(string? source, CancellationToken cancellationToken)
            => Service.ThrowIfContainsErrorNonComputeAsync(source ?? "", cancellationToken);
    }

    [JsonifyErrors(RewriteErrors = true, Order = 1)]
    public class EdgeCaseRewriteController : EdgeCaseController
    {
        public EdgeCaseRewriteController(IEdgeCaseService service) : base(service) { }
    }
}
