using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Configuration;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions.Commands;
using Stl.Text;

namespace Stl.Fusion.Extensions
{
    public interface IIsolatedKeyValueStore
    {
        public static ListFormat ListFormat { get; } = IKeyValueStore.ListFormat;
        public static char Delimiter => IKeyValueStore.Delimiter;

        [CommandHandler]
        Task Set(IsolatedSetCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task SetMany(IsolatedSetManyCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task Remove(IsolatedRemoveCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task RemoveMany(IsolatedRemoveManyCommand command, CancellationToken cancellationToken = default);

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
}
