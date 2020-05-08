using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Channels;
using Stl.Fusion.Publish.Internal;
using Stl.Fusion.Publish.Messages;
using Stl.OS;
using Stl.Reflection;
using Stl.Security;
using Stl.Text;

namespace Stl.Fusion.Publish
{
    public interface IPublisher
    {
        Symbol Id { get; }
        IPublication Publish(IComputed computed, PublicationFactory? publicationFactory = null);
        IPublication? TryGet(Symbol publicationId);
        Task UnpublishAsync(IPublication publication);
    }

    public interface IPublisherImpl : IPublisher
    {
        IChannelRegistry<Message> ChannelRegistry { get; }
        Task<bool> SubscribeAsync(IChannel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken = default);
        Task<bool> UnsubscribeAsync(IChannel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken = default);
        bool BeginDelayedUnpublish(IPublication publication);
        void EndDelayedUnpublish(IPublication publication);
    }

    public abstract class PublisherBase : AsyncDisposableBase, IPublisherImpl, IChannelHandler<Message>
    {
        protected ConcurrentDictionary<(ComputedInput Input, PublicationFactory Factory), PublicationInfo> Publications { get; } 
        protected ConcurrentDictionary<Symbol, PublicationInfo> PublicationsById { get; }
        protected ConcurrentDictionary<IChannel<Message>, ChannelInfo> Channels { get; }
        protected IGenerator<Symbol> PublicationIdGenerator { get; }
        protected bool OwnsChannelRegistry { get; }
        protected Action<IChannel<Message>> OnRegisteredCached { get; } 

        public Symbol Id { get; }
        public IChannelRegistry<Message> ChannelRegistry { get; }
        public PublicationFactory DefaultPublicationFactory { get; }

        protected PublisherBase(Symbol id, 
            IChannelRegistry<Message> channelRegistry,
            IGenerator<Symbol> publicationIdGenerator,
            bool ownsChannelRegistry = true,
            PublicationFactory? defaultPublicationFactory = null)
        {
            defaultPublicationFactory ??= PublicationFactoryEx.Updating;
            Id = id;
            ChannelRegistry = channelRegistry;
            OwnsChannelRegistry = ownsChannelRegistry;
            PublicationIdGenerator = publicationIdGenerator;
            DefaultPublicationFactory = defaultPublicationFactory;
            OnRegisteredCached = OnRegistered;

            var concurrencyLevel = HardwareInfo.ProcessorCount << 2;
            var capacity = 7919;
            Publications = new ConcurrentDictionary<(ComputedInput Input, PublicationFactory Factory), PublicationInfo>(concurrencyLevel, capacity);
            PublicationsById = new ConcurrentDictionary<Symbol, PublicationInfo>(concurrencyLevel, capacity);
            Channels = new ConcurrentDictionary<IChannel<Message>, ChannelInfo>(concurrencyLevel, capacity);
            ChannelRegistry.Registered += OnRegisteredCached;
        }

        public virtual IPublication Publish(IComputed computed, PublicationFactory? publicationFactory = null)
        {
            ThrowIfDisposedOrDisposing();
            publicationFactory ??= DefaultPublicationFactory;
            var spinWait = new SpinWait();
            PublicationInfo pInfo;
            while (true) {
                 pInfo = Publications.GetOrAddChecked(
                    (computed.Input, Factory: publicationFactory), 
                    (key, arg) => {
                        var (this1, computed1) = arg;
                        var publicationId = this1.PublicationIdGenerator.Next();
                        var publication1 = key.Factory.Invoke(this1, computed1, publicationId);
                        var pInfo1 = new PublicationInfo(publication1, key.Factory);
                        this1.PublicationsById[publicationId] = pInfo1;
                        pInfo1.PublishTask = this1.PublishAndUnpublishAsync(pInfo1);
                        return pInfo1;
                    }, (this, computed));
                var publication = pInfo.Publication;
                if (publication.HasHandlers || BeginDelayedUnpublish(publication))
                    break;
                // Couldn't begin delayed dispose means it is already disposing / disposed
                spinWait.SpinOnce();
            }
            // Here we know that either:
            // a) a few moments ago there were handlers, so publication will stay alive for at least as much as 
            //    the new one will (with zero handlers, for which BeginDelayedDispose is called) 
            // b) or we "bumped up" its lifetime by calling BeginDelayedDispose - this happens when it's
            //    either a new publication, or an old one, but with zero handlers
            return pInfo.Publication;                          
        }

        protected virtual async Task PublishAndUnpublishAsync(PublicationInfo info)
        {
            try {
                await info.PublicationImpl.PublishAsync(info.StopCts.Token).ConfigureAwait(false);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch {}
            // TODO: Add assertion / logging for State == Unpublished ?
            info.StopCts?.Dispose();
            info.StopDelayedUnpublishCts?.Dispose();
            Publications.TryRemove((info.Input, info.Factory), info);
            PublicationsById.TryRemove(info.Id, info);
        }

        public virtual IPublication? TryGet(Symbol publicationId) 
            => PublicationsById.TryGetValue(publicationId, out var info) ? info.Publication : null;

        public virtual async Task UnpublishAsync(IPublication publication)
        {
            if (!PublicationsById.TryGetValue(publication.Id, out var info))
                return;
            if (info.State == PublicationState.Unpublished)
                return;
            try {
                info.StopCts.Cancel();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
            try {
                await info.PublishTask.ConfigureAwait(false);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
        }

        public virtual bool BeginDelayedUnpublish(IPublication publication)
        {
            if (!PublicationsById.TryGetValue(publication.Id, out var info))
                return false;
            if (info.State == PublicationState.Unpublished)
                return false;
            var cts = new CancellationTokenSource();
            EndDelayedUnpublish(info);
            if (Interlocked.CompareExchange(ref info.StopDelayedUnpublishCts, cts, null) != null) {
                cts.Dispose();
                return true; // Someone else just did the same, which is fine too
            }
            // We want to just start this task here.
            // No try-catch because the callee is supposed to
            // suppress OperationCancelledException.
            info.PublicationImpl.DelayedUnpublishAsync(cts.Token);
            return true;
        }

        public virtual void EndDelayedUnpublish(IPublication publication)
        {
            if (PublicationsById.TryGetValue(publication.Id, out var info))
                EndDelayedUnpublish(info);
        }

        protected void EndDelayedUnpublish(PublicationInfo info)
        {
            var cts = Interlocked.Exchange(ref info.StopDelayedUnpublishCts, null);
            info.StopDelayedUnpublishCts = null;
            // ReSharper disable once EmptyGeneralCatchClause
            try { cts?.Cancel(); } catch {}
            cts?.Dispose();
        }

        // Channel-related

        protected virtual void OnRegistered(IChannel<Message> channel)
        {
            var channelInfo = new ChannelInfo(channel, this);
            if (!Channels.TryAdd(channel, channelInfo))
                return;
            channel.AddHandler(this);
        }

        Task IChannelHandler<Message>.OnMessageReceivedAsync(IChannel<Message> channel, Message message, CancellationToken cancellationToken) 
            => OnMessageReceivedAsync(channel, message, cancellationToken);
        protected virtual Task OnMessageReceivedAsync(IChannel<Message> channel, Message message, CancellationToken cancellationToken)
        {
            switch (message) {
            case SubscribeMessage sm:
                if (sm.PublisherId != Id)
                    break;
                var publication = TryGet(sm.PublicationId);
                if (publication == null)
                    break;
                return SubscribeAsync(channel, publication, true, cancellationToken);
            case UnsubscribeMessage um:
                if (um.PublisherId != Id)
                    break;
                publication = TryGet(um.PublicationId);
                if (publication == null)
                    break;
                return UnsubscribeAsync(channel, publication, true, cancellationToken);
            }
            return Task.CompletedTask;
        }

        Task<bool> IPublisherImpl.SubscribeAsync(IChannel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken) 
            => SubscribeAsync(channel, publication, notify, cancellationToken);
        protected Task<bool> SubscribeAsync(IChannel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposedOrDisposing();
            if (!Channels.TryGetValue(channel, out var channelInfo))
                return TaskEx.FalseTask;
            if (publication.Publisher != this || publication.State == PublicationState.Unpublished)
                return TaskEx.FalseTask;
            var publicationId = publication.Id;
            if (!channelInfo.Subscriptions.TryAdd(publicationId, default))
                return TaskEx.FalseTask;
            if (!publication.AddHandler(channelInfo)) {
                channelInfo.Subscriptions.TryRemove(publicationId, default);
                return TaskEx.FalseTask;
            }
            if (notify) {
                var message = new SubscribeMessage() {
                    PublisherId = Id,
                    PublicationId = publicationId,
                };
                return channel.SendAsync(message, cancellationToken).ContinueWith(_ => true, cancellationToken);
            }
            return TaskEx.TrueTask;
        }

        Task<bool> IPublisherImpl.UnsubscribeAsync(IChannel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken) 
            => UnsubscribeAsync(channel, publication, notify, cancellationToken);
        protected Task<bool> UnsubscribeAsync(IChannel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken = default)
        {
            if (!Channels.TryGetValue(channel, out var channelInfo))
                return TaskEx.FalseTask;
            var publicationId = publication.Id;
            if (!channelInfo.Subscriptions.TryRemove(publicationId, default))
                return TaskEx.FalseTask;
            publication.RemoveHandler(channelInfo);
            if (notify) {
                var message = new UnsubscribeMessage() {
                    PublisherId = Id,
                    PublicationId = publicationId,
                };
                return channel.SendAsync(message, cancellationToken).ContinueWith(_ => true, cancellationToken);
            }
            return TaskEx.TrueTask;
        }

        void IChannelHandler<Message>.OnDisconnected(IChannel<Message> channel) 
            => OnDisconnected(channel);
        protected virtual void OnDisconnected(IChannel<Message> channel)
        {
            if (!Channels.TryGetValue(channel, out var channelInfo))
                return;
            channel.RemoveHandler(this);
            Task.Run(async () => {
                // We can unsubscribe asynchronously
                var subscriptions = channelInfo.Subscriptions;
                for (var i = 0; i < 2; i++) {
                    while (!subscriptions.IsEmpty) {
                        var tasks = subscriptions
                            .Take(HardwareInfo.ProcessorCount * 4)
                            .ToList()
                            .Select(p => Task.Run(async () => {
                                var (publicationId, _) = (p.Key, p.Value);
                                var publication = TryGet(publicationId);
                                if (publication != null)
                                    await UnsubscribeAsync(channel, publication, false).ConfigureAwait(false);
                            }));
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    // We repeat this twice in case some subscriptions were
                    // still processing after removal of channel's handler.
                    // Since we don't know for sure how long it might take,
                    // we optimistically assume 10 seconds is enough for this.
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
                Channels.TryRemove(channel, channelInfo);
            });
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            ChannelRegistry.Registered -= OnRegisteredCached;
            var publications = PublicationsById;
            while (!publications.IsEmpty) {
                var tasks = publications
                    .Take(HardwareInfo.ProcessorCount * 4)
                    .ToList()
                    .Select(p => Task.Run(async () => {
                        var (_, publicationInfo) = (p.Key, p.Value);
                        await UnpublishAsync(publicationInfo.Publication).ConfigureAwait(false);
                    }));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            if (OwnsChannelRegistry)
                await ChannelRegistry.DisposeAsync().ConfigureAwait(false);
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
        }
    }
}
