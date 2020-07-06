using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComposerController : FusionController, IComposerService
    {
        private readonly IComposerService _composer;

        public ComposerController(IComposerService composer, IPublisher publisher)
            : base(publisher) 
            => _composer = composer;

        [HttpGet("get")]
        public Task<ComposedValue> GetComposedValueAsync(string? parameter, CancellationToken cancellationToken = default) 
        {
            parameter ??= "";
            return PublishAsync(ct => _composer.GetComposedValueAsync(parameter, ct));
        }
    }
}
