#if NETCOREAPP
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Http;
using ControllerBase = System.Web.Http.ApiController;
#endif
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services;

[JsonifyErrors]
public class StringKeyValueController : ControllerBase
{
    protected IKeyValueService<string> Service { get; }

    public StringKeyValueController(IKeyValueService<string> service) => Service = service;

    [HttpGet, Publish]
    public Task<Option<string>> TryGet(string? key)
        => Service.TryGet(key ?? "", this.RequestAborted());

    [HttpGet, Publish]
    public async Task<JsonString> Get(string? key)
        => (await Service.Get(key ?? "", this.RequestAborted()))!;

    [HttpPost]
    public async Task Set(string? key)
    {
#if NETCOREAPP
        using var reader = new StreamReader(Request.Body);
        var value = await reader.ReadToEndAsync();
#else
        var value = await Request.Content.ReadAsStringAsync();
#endif
        await Service.Set(key ?? "", value ?? "", this.RequestAborted());
    }

    [HttpGet]
    public Task Remove(string? key)
        => Service.Remove(key ?? "", this.RequestAborted());

    [HttpPost]
    public Task SetCmd([FromBody] IKeyValueService<string>.SetCommand cmd)
        => Service.SetCmd(cmd, this.RequestAborted());

    [HttpPost]
    public virtual Task RemoveCmd([FromBody] IKeyValueService<string>.RemoveCommand cmd)
        => Service.RemoveCmd(cmd, this.RequestAborted());
}
