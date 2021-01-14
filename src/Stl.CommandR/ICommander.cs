using System.Threading;
using System.Threading.Tasks;
using Stl.DependencyInjection;

namespace Stl.CommandR
{
    public interface ICommander : IHasServices
    {
        CommandContext Start(ICommand command, bool isolate, CancellationToken cancellationToken = default);
        Task<CommandContext> RunAsync(ICommand command, bool isolate, CancellationToken cancellationToken = default);
    }
}
