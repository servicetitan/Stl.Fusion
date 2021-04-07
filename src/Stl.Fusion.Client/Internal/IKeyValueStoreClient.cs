using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Extensions;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Client.Internal
{
    [BasePath("fusion/kvs")]
    public interface IKeyValueStoreClient
    {
        [Post(nameof(Set))]
        Task Set([Body] SetCommand command, CancellationToken cancellationToken = default);
        [Post(nameof(SetMany))]
        Task SetMany([Body] SetManyCommand command, CancellationToken cancellationToken = default);
        [Post(nameof(Remove))]
        Task Remove([Body] RemoveCommand command, CancellationToken cancellationToken = default);
        [Post(nameof(RemoveMany))]
        Task RemoveMany([Body] RemoveManyCommand command, CancellationToken cancellationToken = default);

        [Get(nameof(TryGet))]
        Task<string?> TryGet(string key, CancellationToken cancellationToken = default);
        [Get(nameof(Count))]
        Task<int> Count(string prefix, CancellationToken cancellationToken = default);
        [Get(nameof(ListKeySuffixes))]
        Task<string[]> ListKeySuffixes(
            string prefix,
            PageRef<string> pageRef,
            SortDirection sortDirection = SortDirection.Ascending,
            CancellationToken cancellationToken = default);
    }
}
