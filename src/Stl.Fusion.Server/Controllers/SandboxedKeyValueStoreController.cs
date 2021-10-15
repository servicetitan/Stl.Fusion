using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Server.Controllers;

[Route("fusion/kvs/[action]")]
[ApiController, JsonifyErrors(RewriteErrors = true)]
public class SandboxedKeyValueStoreController : ControllerBase, ISandboxedKeyValueStore
{
    protected ISandboxedKeyValueStore Store { get; }
    protected ISessionResolver SessionResolver { get; }

    public SandboxedKeyValueStoreController(
        ISandboxedKeyValueStore store,
        ISessionResolver sessionResolver)
    {
        Store = store;
        SessionResolver = sessionResolver;
    }

    // Commands

    [HttpPost]
    public Task Set([FromBody] SandboxedSetCommand command, CancellationToken cancellationToken = default)
        => Store.Set(command.UseDefaultSession(SessionResolver), cancellationToken);

    [HttpPost]
    public Task SetMany([FromBody] SandboxedSetManyCommand command, CancellationToken cancellationToken = default)
        => Store.SetMany(command.UseDefaultSession(SessionResolver), cancellationToken);

    [HttpPost]
    public Task Remove([FromBody] SandboxedRemoveCommand command, CancellationToken cancellationToken = default)
        => Store.Remove(command.UseDefaultSession(SessionResolver), cancellationToken);

    [HttpPost]
    public Task RemoveMany([FromBody] SandboxedRemoveManyCommand command, CancellationToken cancellationToken = default)
        => Store.RemoveMany(command.UseDefaultSession(SessionResolver), cancellationToken);

    // Queries

    [HttpGet, Publish]
    public Task<string?> TryGet(Session session, string key, CancellationToken cancellationToken = default)
        => Store.TryGet(session, key, cancellationToken);

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
