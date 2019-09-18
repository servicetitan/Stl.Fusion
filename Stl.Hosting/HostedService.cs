using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Stl.Hosting
{
    public sealed class HostedService<TImpl> : IHostedService
        where TImpl : IHostedService
    {
        public TImpl Implementation { get; }

        public HostedService(TImpl implementation) => Implementation = implementation;

        public Task StartAsync(CancellationToken cancellationToken) 
            => Implementation.StartAsync(cancellationToken);
        public Task StopAsync(CancellationToken cancellationToken) 
            => Implementation.StopAsync(cancellationToken);
    }
}
