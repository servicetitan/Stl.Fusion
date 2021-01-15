using System.Threading;
using System.Threading.Tasks;

namespace Stl.CommandR.Commands
{
    public interface IPreparedCommand : ICommand
    {
        void Prepare(CommandContext context);
    }

    public interface IAsyncPreparedCommand : ICommand
    {
        Task PrepareAsync(CommandContext context, CancellationToken cancellationToken);
    }
}
