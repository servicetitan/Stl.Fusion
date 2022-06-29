using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Server.Controllers;

[Route("fusion/kvs/[action]")]
[ApiController, JsonifyErrors(RewriteErrors = true), UseDefaultSession]
public class SandboxedKeyValueStoreController : ControllerBase, ISandboxedKeyValueStore
{
    private ISandboxedKeyValueStore Store { get; }
    private ICommander Commander { get; }

    public SandboxedKeyValueStoreController(ISandboxedKeyValueStore store, ICommander commander)
    {
        Store = store;
        Commander = commander;
    }

    // Commands

    [HttpPost]
    public Task Set([FromBody] SandboxedSetCommand command, CancellationToken cancellationToken = default)
        => Commander.Call(command, cancellationToken);

    [HttpPost]
    public Task SetMany([FromBody] SandboxedSetCommand command, CancellationToken cancellationToken = default)
        => Commander.Call(command, cancellationToken);

    [HttpPost]
    public Task Remove([FromBody] SandboxedRemoveCommand command, CancellationToken cancellationToken = default)
        => Commander.Call(command, cancellationToken);

    [HttpPost]
    public Task RemoveMany([FromBody] SandboxedRemoveCommand command, CancellationToken cancellationToken = default)
        => Commander.Call(command, cancellationToken);

    // Queries

    [HttpGet, Publish]
    public Task<string?> Get(Session session, string key, CancellationToken cancellationToken = default)
        => Store.Get(session, key, cancellationToken);

    [HttpGet, Publish]
    public Task<int> Count(Session session, string prefix, CancellationToken cancellationToken = default)
        => Store.Count(session, prefix, cancellationToken);

    [HttpGet, Publish]
    public Task<string[]> ListKeySuffixes(
        Session session,
        string prefix,
        PageRef<string> pageRef,
        SortDirection sortDirection = SortDirection.Ascending,
        CancellationToken cancellationToken = default)
        => Store.ListKeySuffixes(session, prefix, pageRef, sortDirection, cancellationToken);
}
