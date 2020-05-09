using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Channels;
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
        IChannelHub<Message> ChannelHub { get; }
        Task<bool> SubscribeAsync(Channel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken = default);
        Task<bool> UnsubscribeAsync(Channel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken = default);
        bool BeginDelayedUnpublish(IPublication publication);
        void EndDelayedUnpublish(IPublication publication);
    }

    public abstract class PublisherBase : AsyncDisposableBase, IPublisherImpl
    {
        protected ConcurrentDictionary<(ComputedInput Input, PublicationFactory Factory), PublicationInfo> Publications { get; } 
        protected ConcurrentDictionary<Symbol, PublicationInfo> PublicationsById { get; }
        protected ConcurrentDictionary<Channel<Message>, ChannelProcessor> ChannelProcessors { get; }
        protected IGenerator<Symbol> PublicationIdGenerator { get; }
        protected bool OwnsChannelRegistry { get; }
        protected Action<Channel<Message>> OnChannelAttachedCached { get; } 
        protected Func<Channel<Message>, ValueTask> OnChannelDetachedCached { get; } 

        public Symbol Id { get; }
        public IChannelHub<Message> ChannelHub { get; }
        public PublicationFactory DefaultPublicationFactory { get; }

        protected PublisherBase(Symbol id, 
            IChannelHub<Message> channelHub,
            IGenerator<Symbol> publicationIdGenerator,
            bool ownsChannelRegistry = true,
            PublicationFactory? defaultPublicationFactory = null)
        {
            defaultPublicationFactory ??= PublicationFactoryEx.Updating;
            Id = id;
            ChannelHub = channelHub;
            OwnsChannelRegistry = ownsChannelRegistry;
            PublicationIdGenerator = publicationIdGenerator;
            DefaultPublicationFactory = defaultPublicationFactory;
            OnChannelAttachedCached = OnChannelAttached;
            OnChannelDetachedCached = OnChannelDetachedAsync;

            var concurrencyLevel = HardwareInfo.ProcessorCount << 2;
            var capacity = 7919;
            Publications = new ConcurrentDictionary<(ComputedInput Input, PublicationFactory Factory), PublicationInfo>(concurrencyLevel, capacity);
            PublicationsById = new ConcurrentDictionary<Symbol, PublicationInfo>(concurrencyLevel, capacity);
            ChannelProcessors = new ConcurrentDictionary<Channel<Message>, ChannelProcessor>(concurrencyLevel, capacity);
            ChannelHub.Attached += OnChannelAttachedCached;
            ChannelHub.Detached += OnChannelDetachedCached;
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
                await info.PublicationImpl.PublishAsync(info.StopToken).ConfigureAwait(false);
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
            using var cts = new CancellationTokenSource();
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

        protected virtual void OnChannelAttached(Channel<Message> channel)
        {
            var channelProcessor = new ChannelProcessor(channel, this);
            if (!ChannelProcessors.TryAdd(channel, channelProcessor))
                return;
            channelProcessor.RunAsync().ContinueWith(_ => {
                // Since ChannelProcessor is AsyncProcessorBase desc.,
                // its disposal will shut down RunAsync as well,
                // so "subscribing" to RunAsync completion is the
                // same as subscribing to its disposal.
                ChannelProcessors.TryRemove(channel, channelProcessor);
            });
        }

        protected virtual ValueTask OnChannelDetachedAsync(Channel<Message> channel)
        {
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return ValueTaskEx.CompletedTask;
            return channelProcessor.DisposeAsync();
        }

        Task<bool> IPublisherImpl.SubscribeAsync(Channel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken) 
            => SubscribeAsync(channel, publication, notify, cancellationToken);
        protected Task<bool> SubscribeAsync(Channel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposedOrDisposing();
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return TaskEx.FalseTask;
            if (publication.Publisher != this || publication.State == PublicationState.Unpublished)
                return TaskEx.FalseTask;
            var publicationId = publication.Id;
            if (!channelProcessor.Subscriptions.TryAdd(publicationId, default))
                return TaskEx.FalseTask;
            if (!publication.AddHandler(channelProcessor)) {
                channelProcessor.Subscriptions.TryRemove(publicationId, default);
                return TaskEx.FalseTask;
            }
            if (notify) {
                var message = new SubscribeMessage() {
                    PublisherId = Id,
                    PublicationId = publicationId,
                };
                return channel.Writer.WriteAsync(message, cancellationToken)
                    .AsTask().ContinueWith(_ => true, CancellationToken.None);
            }
            return TaskEx.TrueTask;
        }

        Task<bool> IPublisherImpl.UnsubscribeAsync(Channel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken) 
            => UnsubscribeAsync(channel, publication, notify, cancellationToken);
        protected Task<bool> UnsubscribeAsync(Channel<Message> channel, IPublication publication, bool notify, CancellationToken cancellationToken = default)
        {
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return TaskEx.FalseTask;
            var publicationId = publication.Id;
            if (!channelProcessor.Subscriptions.TryRemove(publicationId, default))
                return TaskEx.FalseTask;
            publication.RemoveHandler(channelProcessor);
            if (notify) {
                var message = new UnsubscribeMessage() {
                    PublisherId = Id,
                    PublicationId = publicationId,
                };
                return channel.Writer.WriteAsync(message, cancellationToken)
                    .AsTask().ContinueWith(_ => true, CancellationToken.None);
            }
            return TaskEx.TrueTask;
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            ChannelHub.Attached -= OnChannelAttachedCached;
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
                await ChannelHub.DisposeAsync().ConfigureAwait(false);
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
        }
    }
}
