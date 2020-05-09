using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.OS;

namespace Stl.Channels
{
    public interface IChannelHub<T> : IAsyncDisposable
    {
        bool Attach(Channel<T> channel);
        bool IsAttached(Channel<T> channel);
        ValueTask<bool> DetachAsync(Channel<T> channel);

        event Action<Channel<T>> Attached;
        event Func<Channel<T>, ValueTask> Detached;
    }

    public class ChannelHub<T> : AsyncDisposableBase, IChannelHub<T>
    {
        protected ConcurrentDictionary<Channel<T>, Unit> Channels { get; }
        protected ConcurrentDictionary<Action<Channel<T>>, Unit> AttachedHandlers { get; }
        protected ConcurrentDictionary<Func<Channel<T>, ValueTask>, Unit> DetachedHandlers { get; }

        public event Action<Channel<T>> Attached {
            add {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                ThrowIfDisposedOrDisposing();
                AttachedHandlers.TryAdd(value, Unit.Default);
            }
            remove => AttachedHandlers.TryRemove(value, out _);
        }

        public event Func<Channel<T>, ValueTask> Detached {
            add {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                ThrowIfDisposedOrDisposing();
                DetachedHandlers.TryAdd(value, Unit.Default);
            }
            remove => DetachedHandlers.TryRemove(value, out _);
        }

        public ChannelHub()
        {
            var concurrencyLevel = HardwareInfo.ProcessorCount << 2;
            var capacity = 7919;
            Channels = new ConcurrentDictionary<Channel<T>, Unit>(concurrencyLevel, capacity);
            AttachedHandlers = new ConcurrentDictionary<Action<Channel<T>>, Unit>();
            DetachedHandlers = new ConcurrentDictionary<Func<Channel<T>, ValueTask>, Unit>();
        }

        public virtual bool Attach(Channel<T> channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            ThrowIfDisposedOrDisposing();
            
            if (!Channels.TryAdd(channel, default))
                return false;
            OnAttached(channel);
            return true;
        }

        public virtual async ValueTask<bool> DetachAsync(Channel<T> channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            ThrowIfDisposedOrDisposing();
            
            if (!Channels.TryRemove(channel, out _))
                return false;
            await OnDetachedAsync(channel).ConfigureAwait(false);
            return true;
        }

        public bool IsAttached(Channel<T> channel) 
            => Channels.TryGetValue(channel, out _);

        protected virtual void OnAttached(Channel<T> channel)
        {
            channel.Reader.Completion.ContinueWith(async _ => {
                await DetachAsync(channel);
            });

            // TODO: Add exception aggregation?
            foreach (var (handler, _) in AttachedHandlers) {
                try {
                    handler.Invoke(channel);
                }
                catch {
                    // Ignore: we want to invoke all handlers
                }
            }
        }

        protected virtual async ValueTask OnDetachedAsync(Channel<T> channel)
        {
            // TODO: Add exception aggregation?

            // Let's try to run all of them in parallel
            var tasks = new List<ValueTask>();
            foreach (var (handler, _) in DetachedHandlers) {
                try {
                    var valueTask = handler.Invoke(channel);
                    if (!valueTask.IsCompleted)
                        tasks.Add(valueTask);
                }
                catch {
                    // Ignore: we want to invoke all handlers
                }
            }
            foreach (var task in tasks) {
                try {
                    await task.ConfigureAwait(false);
                }
                catch {
                    // Ignore: we want to invoke all handlers
                }
            }

            switch (channel) {
            case IAsyncDisposable ad:
                await ad.DisposeAsync().ConfigureAwait(false);
                break;
            case IDisposable d:
                d.Dispose();
                break;
            default:
                for (var i = 0; i < 3; i++) {
                    if (channel.Writer.TryComplete())
                        break;
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
                break;
            }
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            while (!Channels.IsEmpty) {
                var tasks = Channels
                    .Take(HardwareInfo.ProcessorCount * 4)
                    .ToList()
                    .Select(p => Task.Run(async () => {
                        var channel = p.Key;
                        if (!Channels.TryRemove(channel, out _))
                            return;
                        try {
                            await OnDetachedAsync(channel).ConfigureAwait(false);
                        }
                        catch {
                            // Ignore: we did what we could, Dispose shouldn't throw anything
                        }
                    }));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
    }
}
