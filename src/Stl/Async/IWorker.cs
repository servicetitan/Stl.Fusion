using Microsoft.Extensions.Hosting;

namespace Stl.Async;

public interface IWorker : IAsyncDisposable, IDisposable, IHasWhenDisposed, IHostedService
{
    Task? WhenRunning { get; }

    Task Run();
    Task Stop();
}
