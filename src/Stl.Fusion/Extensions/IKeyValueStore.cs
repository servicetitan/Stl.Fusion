using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Configuration;
using Stl.Fusion.Extensions.Commands;
using Stl.Text;

namespace Stl.Fusion.Extensions
{
#if NETSTANDARD2_0
    public class IKeyValueStoreFields
    {
        public static ListFormat ListFormat { get; } = ListFormat.SlashSeparated;
        public static char Delimiter => ListFormat.Delimiter;
    }
#endif
    
    public interface IKeyValueStore
    {
#if !NETSTANDARD2_0       
        public static ListFormat ListFormat { get; } = ListFormat.SlashSeparated;
        public static char Delimiter => ListFormat.Delimiter;
#endif

        [CommandHandler]
        Task Set(SetCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task SetMany(SetManyCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task Remove(RemoveCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task RemoveMany(RemoveManyCommand command, CancellationToken cancellationToken = default);

        [ComputeMethod]
        Task<string?> TryGet(string key, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<int> Count(string prefix, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<string[]> ListKeySuffixes(
            string prefix,
            PageRef<string> pageRef,
            SortDirection sortDirection = SortDirection.Ascending,
            CancellationToken cancellationToken = default);
    }
}
