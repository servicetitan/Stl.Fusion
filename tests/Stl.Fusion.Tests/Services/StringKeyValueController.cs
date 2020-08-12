using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class StringKeyValueController : FusionController, IKeyValueService<string>
    {
        protected IKeyValueService<string> Service { get; }

        public StringKeyValueController(IPublisher publisher, IKeyValueService<string> service) : base(publisher)
            => Service = service;

        [HttpGet("{key?}")]
        public Task<Option<string>> GetValueAsync(string? key, CancellationToken cancellationToken = default)
            => PublishAsync(ct => Service.GetValueAsync(key ?? "", ct), cancellationToken);

        [HttpPost("{key?}")]
        public Task SetValueAsync(string? key, [FromBody] Option<string> value, CancellationToken cancellationToken = default)
            => Service.SetValueAsync(key ?? "", value, cancellationToken);
    }
}
