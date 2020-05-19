using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Stl.Hosting.Internal
{
    public sealed class HostedServiceWrapper<TImpl> : IHostedService
        where TImpl : IHostedService
    {
        public TImpl Implementation { get; }

        public HostedServiceWrapper(TImpl implementation) => Implementation = implementation;

        public Task StartAsync(CancellationToken cancellationToken) 
            => Implementation.StartAsync(cancellationToken);
        public Task StopAsync(CancellationToken cancellationToken) 
            => Implementation.StopAsync(cancellationToken);
    }
}
