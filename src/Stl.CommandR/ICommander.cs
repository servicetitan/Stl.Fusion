using System.Threading;
using System.Threading.Tasks;
using Stl.DependencyInjection;

namespace Stl.CommandR
{
    public interface ICommander : IHasServices
    {
        Task Run(CommandContext context, bool isolate, CancellationToken cancellationToken = default);
    }
}
