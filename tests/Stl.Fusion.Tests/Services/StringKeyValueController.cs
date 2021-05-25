using System.IO;
using System.Threading;
using System.Threading.Tasks;
#if NETCOREAPP
using Microsoft.AspNetCore.Mvc;
#else
using System.Web;
using System.Web.Http;
#endif

using Stl.Fusion.Server;
using Stl.Serialization;

namespace Stl.Fusion.Tests.Services
{
#if NETCOREAPP
    
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

#else

    [RoutePrefix("api/stringKeyValue")]
    [JsonifyErrors]
    public class StringKeyValueController : ApiController
    {
        protected IKeyValueService<string> Service { get; }

        public StringKeyValueController(IKeyValueService<string> service) => Service = service;

        [HttpGet, Publish]
        [Route("tryGet/{key?}")]
        public Task<Option<string>> TryGet(string? key = null)
            => Service.TryGet(key ?? "" /*, ActionContext.RequestAborted()*/);

        [HttpGet, Publish]
        [Route("get/{key?}")]
        public async Task<JsonString> Get(string? key = null)
            => await Service.Get(key ?? "" /*, ActionContext.RequestAborted()*/);

        [HttpPost]
        [Route("set/{key?}")]
        public async Task Set(string? key = null)
        {
            var value = await ActionContext.Request.Content.ReadAsStringAsync();
            await Service.Set(key ?? "", value ?? "", ActionContext.RequestAborted());
        }

        [HttpGet]
        [Route("remove/{key?}")]
        public Task Remove(string? key = null)
            => Service.Remove(key ?? "", ActionContext.RequestAborted());

        [HttpPost]
        [Route("setCmd")]
        public Task SetCmd([FromBody] IKeyValueService<string>.SetCommand cmd)
            => Service.SetCmd(cmd, ActionContext.RequestAborted());

        [HttpPost]
        [Route("removeCmd")]
        public virtual Task RemoveCmd([FromBody] IKeyValueService<string>.RemoveCommand cmd)
            => Service.RemoveCmd(cmd, ActionContext.RequestAborted());
    }
    
#endif
}
