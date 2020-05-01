using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections.Slim;
using Stl.Text;

namespace Stl.Fusion.Publication
{
    public interface IClient : IAsyncDisposable
    {
        Symbol Key { get; }
        ValueTask SendAsync(object message, CancellationToken cancellationToken);
        event Action<IClient> Disconnected;
    }

    public abstract class ClientBase : AsyncDisposableBase, IClient
    {
        // There can be lots of handlers, so we can't use the default impl.
        private RefHashSetSlim4<Action<IClient>> _disconnectedHandlers = default;

        public Symbol Key { get; }

        protected ClientBase(Symbol id) => Key = id;

        public abstract ValueTask SendAsync(object message, CancellationToken cancellationToken);

        public event Action<IClient> Disconnected {
            add {
                if (DisposalState == DisposalState.Active)
                    _disconnectedHandlers.Add(value);
                else
                    value?.Invoke(this);
            }
            remove => _disconnectedHandlers.Remove(value);
        }

        protected virtual void OnDisconnected() 
            => _disconnectedHandlers.Apply(this, (self, handler) => handler?.Invoke(self));
    }
}
