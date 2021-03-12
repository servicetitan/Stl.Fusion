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
        public Task<Option<string>> TryGet(string? key)
            => Service.TryGet(key ?? "", HttpContext.RequestAborted);

        [HttpGet("{key?}"), Publish]
        public async Task<JsonString> Get(string? key)
            => await Service.Get(key ?? "", HttpContext.RequestAborted);

        [HttpPost("{key?}")]
        public async Task Set(string? key)
        {
            using var reader = new StreamReader(Request.Body);
            var value = await reader.ReadToEndAsync();
            await Service.Set(key ?? "", value ?? "", HttpContext.RequestAborted);
        }

        [HttpGet("{key?}")]
        public Task Remove(string? key)
            => Service.Remove(key ?? "", HttpContext.RequestAborted);

        [HttpPost]
        public Task SetCmd([FromBody] IKeyValueService<string>.SetCommand cmd)
            => Service.SetCmd(cmd, HttpContext.RequestAborted);

        [HttpPost]
        public virtual Task RemoveCmd([FromBody] IKeyValueService<string>.RemoveCommand cmd)
            => Service.RemoveCmd(cmd, HttpContext.RequestAborted);
    }
}
