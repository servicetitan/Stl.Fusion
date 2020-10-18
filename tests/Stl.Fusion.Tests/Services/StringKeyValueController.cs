using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;
using Stl.Serialization;

namespace Stl.Fusion.Tests.Services
{
    [Route("api/[controller]/[action]")]
    [ApiController, JsonifyErrors]
    public class StringKeyValueController : ControllerBase
    {
        protected IKeyValueService<string> Service { get; }

        public StringKeyValueController(IKeyValueService<string> service) => Service = service;

        [HttpGet("{key?}"), Publish]
        public Task<Option<string>> TryGetAsync(string? key)
            => Service.TryGetAsync(key ?? "", HttpContext.RequestAborted);

        [HttpGet("{key?}"), Publish]
        public async Task<JsonString> GetAsync(string? key)
            => await Service.GetAsync(key ?? "", HttpContext.RequestAborted);

        [HttpPost("{key?}")]
        public async Task SetAsync(string? key)
        {
            using var reader = new StreamReader(Request.Body);
            var value = await reader.ReadToEndAsync();
            await Service.SetAsync(key ?? "", value ?? "", HttpContext.RequestAborted);
        }

        [HttpGet("{key?}")]
        public Task RemoveAsync(string? key)
            => Service.RemoveAsync(key ?? "", HttpContext.RequestAborted);
    }
}
