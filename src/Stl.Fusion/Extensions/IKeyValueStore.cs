using Stl.Fusion.Extensions.Commands;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Extensions;

public interface IKeyValueStore : IComputeService
{
    [CommandHandler]
    Task Set(SetCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Remove(RemoveCommand command, CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<string?> Get(string tenantId, string key, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<int> Count(string tenantId, string prefix, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<string[]> ListKeySuffixes(
        string tenantId,
        string prefix,
        PageRef<string> pageRef,
        SortDirection sortDirection = SortDirection.Ascending,
        CancellationToken cancellationToken = default);
}
