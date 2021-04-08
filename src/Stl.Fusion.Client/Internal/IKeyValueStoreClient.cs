using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Client.Internal
{
    [BasePath("fusion/ikvs")]
    public interface IIsolatedKeyValueStoreClient
    {
        [Post(nameof(Set))]
        Task Set([Body] IsolatedSetCommand command, CancellationToken cancellationToken = default);
        [Post(nameof(SetMany))]
        Task SetMany([Body] IsolatedSetManyCommand command, CancellationToken cancellationToken = default);
        [Post(nameof(Remove))]
        Task Remove([Body] IsolatedRemoveCommand command, CancellationToken cancellationToken = default);
        [Post(nameof(RemoveMany))]
        Task RemoveMany([Body] IsolatedRemoveManyCommand command, CancellationToken cancellationToken = default);

        [Get(nameof(TryGet))]
        Task<string?> TryGet(Session session, string key, CancellationToken cancellationToken = default);
        [Get(nameof(Count))]
        Task<int> Count(Session session, string prefix, CancellationToken cancellationToken = default);
        [Get(nameof(ListKeySuffixes))]
        Task<string[]> ListKeySuffixes(
            Session session,
            string prefix,
            PageRef<string> pageRef,
            SortDirection sortDirection = SortDirection.Ascending,
            CancellationToken cancellationToken = default);
    }
}
