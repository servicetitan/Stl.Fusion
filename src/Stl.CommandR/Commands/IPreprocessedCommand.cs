using System.Threading;
using System.Threading.Tasks;

namespace Stl.CommandR.Commands
{
    public interface IPreprocessedCommand : ICommand
    {
        void Preprocess(CommandContext context);
    }

    public interface IAsyncPreprocessedCommand : ICommand
    {
        Task PreprocessAsync(CommandContext context, CancellationToken cancellationToken);
    }
}
