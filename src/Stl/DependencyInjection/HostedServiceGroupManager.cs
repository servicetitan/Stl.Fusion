using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Stl.DependencyInjection;

/// <summary>
/// Manages a group of <see cref="IHostedService"/>-s as a whole
/// allowing to start or stop all of them.
/// </summary>
public class HostedServiceGroupManager : IHasServices, IEnumerable<IHostedService>
{
    public IServiceProvider Services { get; }

    public HostedServiceGroupManager(IServiceProvider services) => Services = services;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IHostedService> GetEnumerator()
        => Services.GetServices<IHostedService>().GetEnumerator();

    public async Task Start(CancellationToken cancellationToken = default)
    {
        var tasks = this.Select(s => s.StartAsync(cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        var tasks = this.Select(s => s.StopAsync(cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
