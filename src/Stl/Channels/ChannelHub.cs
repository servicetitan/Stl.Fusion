using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;
using Stl.OS;

namespace Stl.Channels
{
    public delegate void ChannelAttachedHandler<T>(Channel<T> channel);
    public delegate void ChannelDetachedHandler<T>(Channel<T> channel, ref Collector<ValueTask> taskCollector);

    public interface IChannelHub<T> : IAsyncDisposable
    {
        int ChannelCount { get; }
        bool Attach(Channel<T> channel);
        bool IsAttached(Channel<T> channel);
        ValueTask<bool> DetachAsync(Channel<T> channel);

        event ChannelAttachedHandler<T> Attached;
        event ChannelDetachedHandler<T> Detached;
    }

    public class ChannelHub<T> : AsyncDisposableBase, IChannelHub<T>
    {
        protected ConcurrentDictionary<Channel<T>, Unit> Channels { get; }

        public int ChannelCount => Channels.Count;
        public event ChannelAttachedHandler<T>? Attached;
        public event ChannelDetachedHandler<T>? Detached;

        public ChannelHub()
        {
            var concurrencyLevel = HardwareInfo.GetProcessorCountFactor(4, 4);
            var capacity = OSInfo.IsWebAssembly ? 17 : 509;
            Channels = new ConcurrentDictionary<Channel<T>, Unit>(concurrencyLevel, capacity);
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
            Attached?.Invoke(channel);
        }

        protected virtual async ValueTask OnDetachedAsync(Channel<T> channel)
        {
            var taskCollector = Collector<ValueTask>.New(true);
            try {
                Detached?.Invoke(channel, ref taskCollector);

                // The traversal direction doesn't matter, so let's traverse
                // it in reverse order to help the compiler to get rid of extra
                // bound checks.
                var tasks = taskCollector.Buffer;
                for (var i = taskCollector.Count - 1; i >= 0; i--) {
                    var task = tasks[i];
                    try {
                        await task.ConfigureAwait(false);
                    }
                    catch {
                        // Ignore: we want to invoke all handlers
                    }
                }
            }
            finally {
                taskCollector.Dispose();
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
                    .Take(HardwareInfo.GetProcessorCountFactor(4, 4))
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
