using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Stl.Async;
using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Publish
{
    public interface IChannelRegistry<TMessage> : IAsyncDisposable
    {
        void Register(IChannel<TMessage> channel);
        IChannel<TMessage>? TryGet(Symbol channelId);
        event Action<IChannel<TMessage>> Registered;
    }

    public class ChannelRegistry<TMessage> : AsyncDisposableBase, IChannelRegistry<TMessage>
    {
        protected ConcurrentDictionary<Symbol, IChannel<TMessage>> Channels { get; }
        protected ConcurrentDictionary<Action<IChannel<TMessage>>, Unit> RegisteredHandlers { get; }

        public ChannelRegistry()
        {
            var concurrencyLevel = HardwareInfo.ProcessorCount << 2;
            var capacity = 7919;
            Channels = new ConcurrentDictionary<Symbol, IChannel<TMessage>>(concurrencyLevel, capacity);
            RegisteredHandlers = new ConcurrentDictionary<Action<IChannel<TMessage>>, Unit>();
        }

        public virtual void Register(IChannel<TMessage> channel)
        {
            ThrowIfDisposedOrDisposing();
            // A bit complex logic is meant to handle reconnection,
            // i.e. registration of a new client with the same
            // IClient.Key
            var channelId = channel.Id;
            while (!Channels.TryAdd(channelId, channel)) {
                while (Channels.TryGetValue(channelId, out var oldChannel)) {
                    if (oldChannel == channel)
                        return;
                    if (Channels.TryUpdate(channelId, channel, oldChannel)) {
                        if (oldChannel != null)
                            Task.Run(oldChannel.DisposeAsync);
                        OnRegistered(channel);
                        return;
                    }
                }
            }
            OnRegistered(channel);
        }

        public virtual IChannel<TMessage>? TryGet(Symbol channelId)
            => Channels.TryGetValue(channelId, out var channel) ? channel : null;

        public event Action<IChannel<TMessage>> Registered {
            add {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                ThrowIfDisposedOrDisposing();
                RegisteredHandlers.TryAdd(value, Unit.Default);
            }
            remove => RegisteredHandlers.TryRemove(value, out _);
        }

        protected virtual void OnRegistered(IChannel<TMessage> channel)
        {
            // TODO: Add exception aggregation?
            foreach (var (handler, _) in RegisteredHandlers)
                handler.Invoke(channel);
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            while (!Channels.IsEmpty) {
                var tasks = Channels
                    .Take(HardwareInfo.ProcessorCount * 4)
                    .ToList()
                    .Select(p => Task.Run(async () => {
                        var (channelId, channel) = (p.Key, p.Value);
                        try {
                            await channel.DisposeAsync().ConfigureAwait(false);
                        }
                        // ReSharper disable once EmptyGeneralCatchClause
                        catch {}
                        Channels.TryRemove(channelId, out _);
                    }));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
    }
}
