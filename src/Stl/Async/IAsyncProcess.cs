using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Stl.Async
{
    public interface IAsyncProcess : IHostedService, IAsyncDisposable, IDisposable, IHasDisposeStarted
    {
        CancellationToken StopToken { get; }
        Task? RunningTask { get; }

        Task Run();
        Task Run(CancellationToken cancellationToken);
        void Start();
        Task Stop();
    }
}
