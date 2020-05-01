using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Stl.Async;
using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Publication
{
    public interface IClientRegistry : IAsyncDisposable
    {
        void Register(IClient client);
        IClient? TryGet(Symbol clientKey);
        event Action<IClient> Connected;
    }

    public class ClientRegistry : AsyncDisposableBase, IClientRegistry
    {
        private readonly ConcurrentDictionary<Action<IClient>, Unit> _connectedHandlers =
            new ConcurrentDictionary<Action<IClient>, Unit>();
        private readonly ConcurrentDictionary<Symbol, IClient> _clients = 
            new ConcurrentDictionary<Symbol, IClient>(); 

        public virtual void Register(IClient client)
        {
            ThrowIfDisposedOrDisposing();
            if (_clients.TryAdd(client.Key, client))
                OnConnected(client);
        }

        public virtual IClient? TryGet(Symbol clientKey)
            => _clients.TryGetValue(clientKey, out var client) ? client : null;

        public event Action<IClient> Connected {
            add {
                ThrowIfDisposedOrDisposing();
                _connectedHandlers.TryAdd(value, Unit.Default);
            }
            remove => _connectedHandlers.TryRemove(value, out _);
        }

        protected virtual void OnConnected(IClient client)
        {
            // TODO: Add exception aggregation?
            foreach (var (handler, _) in _connectedHandlers)
                handler?.Invoke(client);
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            while (!_clients.IsEmpty) {
                var tasks = _clients
                    .Take(HardwareInfo.ProcessorCount * 4)
                    .ToList()
                    .Select(p => Task.Run(async () => {
                        var (clientKey, client) = (p.Key, p.Value);
                        try {
                            await client.DisposeAsync().ConfigureAwait(false);
                        }
                        // ReSharper disable once EmptyGeneralCatchClause
                        catch {}
                        _clients.TryRemove(clientKey, out _);
                    }));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
    }
}
