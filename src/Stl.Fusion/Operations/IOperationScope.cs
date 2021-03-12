using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;

namespace Stl.Fusion.Operations
{
    public interface IOperationScope : IAsyncDisposable
    {
        IOperation Operation { get; }
        CommandContext CommandContext { get; }
        bool IsUsed { get; }
        bool IsClosed { get; }
        bool? IsConfirmed { get; }

        Task Commit(CancellationToken cancellationToken = default);
        Task Rollback();
    }
}
