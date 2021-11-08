using Microsoft.Extensions.Hosting;

namespace Stl.Async;

public interface IAsyncProcess : IHostedService, IAsyncDisposable, IDisposable, IHasDisposeStarted
{
    Task? RunningTask { get; }

    Task Run();
    Task Run(CancellationToken cancellationToken);
    void Start();
    Task Stop();
}
