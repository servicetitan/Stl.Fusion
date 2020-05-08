using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;
using Stl.Collections.Slim;
using Stl.Text;

namespace Stl.Fusion.Channels
{
    public interface IChannel<TMessage> : IAsyncDisposable
    {
        Symbol Id { get; }
        Task SendAsync(TMessage message, CancellationToken cancellationToken);
        bool AddHandler(IChannelHandler<TMessage> handler);
        bool RemoveHandler(IChannelHandler<TMessage> handler);
    }

    public abstract class ChannelBase<TMessage> : AsyncDisposableBase, IChannel<TMessage>
    {
        // There can be lots of handlers, so we can't use the default impl.
        protected RefHashSetSlim4<IChannelHandler<TMessage>> Handlers = default;
        protected object Lock = new object();
        
        public Symbol Id { get; }

        protected ChannelBase(Symbol id) 
            => Id = id;

        public abstract Task SendAsync(TMessage message, CancellationToken cancellationToken);

        public bool AddHandler(IChannelHandler<TMessage> handler)
        {
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            lock (Lock) {
                return Handlers.Add(handler);
            }
        }

        public bool RemoveHandler(IChannelHandler<TMessage> handler)
        {
            lock (Lock) {
                return Handlers.Remove(handler);
            }
        }

        protected virtual Task OnMessageReceived(TMessage message, CancellationToken cancellationToken)
        {
            // The logic is complex to avoid extra allocations
            var bTasks = ListBuffer<Task>.Lease(); 
            ListBuffer<IChannelHandler<TMessage>> bHandlers = default;
            try {
                lock (Lock) {
                    var handlerCount = Handlers.Count;
                    if (handlerCount == 0)
                        return Task.CompletedTask;
                    bHandlers = ListBuffer<IChannelHandler<TMessage>>.LeaseAndSetCount(handlerCount);
                    Handlers.CopyTo(bHandlers.Span);
                }
                foreach (var handler in bHandlers.Span) {
                    try {
                        var task = handler.OnMessageReceivedAsync(this, message, cancellationToken);
                        if (!task.IsCompletedSuccessfully)
                            bTasks.Add(task);
                    }
                    catch {
                        // Ignore: all handlers must do their job
                    }
                }
                return bTasks.Count switch {
                    0 => Task.CompletedTask,
                    1 => bTasks[0],
                    _ => Task.WhenAll(bTasks.ToArray())
                };
            }
            finally {
                bTasks.Release();
                bHandlers.Release();
            }
        }

        protected virtual void OnDisconnected()
        {
            lock (Lock) {
                Handlers.Apply(this, (self, handler) => {
                    try {
                        handler.OnDisconnected(self);
                    }
                    catch {
                        // Ignore: all handlers must do their job
                    }
                });
            }
        }
    }
}
