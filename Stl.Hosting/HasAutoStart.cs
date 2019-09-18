using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Stl.Plugins;

namespace Stl.Hosting 
{
    public sealed class HasAutoStart<TImpl> : IHostedService
        where TImpl : IHasAutoStart
    {
        public TImpl Implementation { get; }

        public HasAutoStart(TImpl implementation) => Implementation = implementation;

        public Task StartAsync(CancellationToken cancellationToken) =>
            Task.Run(() => {
                Implementation.AutoStart();
                return Task.CompletedTask;
            });

        public Task StopAsync(CancellationToken cancellationToken) 
            => Task.CompletedTask;
    }
}
