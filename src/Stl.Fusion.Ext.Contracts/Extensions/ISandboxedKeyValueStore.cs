namespace Stl.Fusion.Extensions;

public interface ISandboxedKeyValueStore : IComputeService
{
    [CommandHandler]
    Task Set(SandboxedKeyValueStore_Set command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Remove(SandboxedKeyValueStore_Remove command, CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<string?> Get(Session session, string key, CancellationToken cancellationToken = default);
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
