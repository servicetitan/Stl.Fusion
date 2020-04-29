using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Stl.Async;

namespace Stl.Hosting.HostedServices 
{
    public sealed class AsyncProcessWrapper<TImpl> : IHostedService
        where TImpl : IAsyncProcess
    {
        public TImpl Implementation { get; }

        public AsyncProcessWrapper(TImpl implementation) => Implementation = implementation;

        public Task StartAsync(CancellationToken cancellationToken) =>
            Task.Run(() => {
                Implementation.RunAsync();
                return Task.CompletedTask;
            });

        public async Task StopAsync(CancellationToken cancellationToken) 
            => await Implementation.DisposeAsync();
    }
}
