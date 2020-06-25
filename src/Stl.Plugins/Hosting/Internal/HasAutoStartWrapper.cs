using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Stl.Plugins.Hosting.Internal 
{
    public sealed class HasAutoStartWrapper<TImpl> : IHostedService
        where TImpl : IHasAutoStart
    {
        public TImpl Implementation { get; }

        public HasAutoStartWrapper(TImpl implementation) => Implementation = implementation;

        public Task StartAsync(CancellationToken cancellationToken) =>
            Task.Run(() => {
                Implementation.AutoStart();
                return Task.CompletedTask;
            });

        public Task StopAsync(CancellationToken cancellationToken) 
            => Task.CompletedTask;
    }
}
