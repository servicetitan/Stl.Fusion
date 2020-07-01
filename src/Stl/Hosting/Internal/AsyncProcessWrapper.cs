using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Stl.Async;

namespace Stl.Hosting.Internal 
{
    public sealed class AsyncProcessWrapper<TService> : IHostedService
        where TService : class
    {
        public IAsyncProcess AsyncProcess { get; }

        public AsyncProcessWrapper(TService service) 
            => AsyncProcess = (IAsyncProcess) service;

        public Task StartAsync(CancellationToken cancellationToken) =>
            Task.Run(() => {
                AsyncProcess.RunAsync();
                return Task.CompletedTask;
            }, CancellationToken.None);

        public async Task StopAsync(CancellationToken cancellationToken) 
            => await AsyncProcess.DisposeAsync();
    }
}
