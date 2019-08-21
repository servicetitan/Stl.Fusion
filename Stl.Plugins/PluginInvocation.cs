using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Stl.Extensibility;

namespace Stl.Plugins
{
    // Supposed to be subclassed
    public class PluginInvocation<TPlugin, TSelf>
        where TSelf : PluginInvocation<TPlugin, TSelf>
    {
        public ImmutableArray<TPlugin> Plugins { get; set; } =
            ImmutableArray<TPlugin>.Empty;
        public HashSet<TPlugin> DisabledPlugins { get; } = 
            new HashSet<TPlugin>();

        public void Invoke(Action<TPlugin, ICallChain<TSelf>> action)
        {
            // ReSharper disable once HeapView.BoxingAllocation
            var state = Plugins.ChainInvoke(
                (TSelf) this, 
                (plugin, chain) => {
                    if (chain.State.DisabledPlugins.Contains(plugin))
                        chain.InvokeNext();
                    else
                        action.Invoke(plugin, chain);
                });
        }

        public void InvokeAsync(
            Func<TPlugin, IAsyncCallChain<TSelf>, CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
        {
            // ReSharper disable once HeapView.BoxingAllocation
            var state = Plugins.ChainInvokeAsync(
                (TSelf) this, 
                (plugin, chain, cancellationToken1) => {
                    if (chain.State.DisabledPlugins.Contains(plugin))
                        return chain.InvokeNextAsync(cancellationToken1);
                    else
                        return action.Invoke(plugin, chain, cancellationToken1);
                }, 
                cancellationToken);
        }
    }
}
