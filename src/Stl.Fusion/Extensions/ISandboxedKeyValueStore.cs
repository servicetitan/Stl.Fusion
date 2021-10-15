using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Extensions;

public interface ISandboxedKeyValueStore
{
    [CommandHandler]
    Task Set(SandboxedSetCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task SetMany(SandboxedSetManyCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Remove(SandboxedRemoveCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task RemoveMany(SandboxedRemoveManyCommand command, CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<string?> TryGet(Session session, string key, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<int> Count(Session session, string prefix, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<string[]> ListKeySuffixes(
        Session session,
        string prefix,
        PageRef<string> pageRef,
        SortDirection sortDirection = SortDirection.Ascending,
        CancellationToken cancellationToken = default);
}
