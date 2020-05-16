using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Channels;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Bridge.Messages;
using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public interface IReproducer
    {
        IChannelHub<PublicationMessage> ChannelHub { get; }
        bool OwnsChannelHub { get; }

        IReproduction<T> TryAddOrGet<T>(Symbol publicationId, Result<T> initialOutput);
        IReproduction TryGet(Symbol publicationId);
    }

    public interface IReproducerImpl : IReproducer
    {
        void OnReproductionDisposed(IReproduction reproduction);
    }

    public class Reproducer : AsyncDisposableBase, IReproducerImpl
    {
        private ConcurrentDictionary<Symbol, IReproduction> Reproductions { get; }
        protected ConcurrentDictionary<Channel<PublicationMessage>, ReproducerChannelProcessor> ChannelProcessors { get; }
        protected Action<Channel<PublicationMessage>> OnChannelAttachedCached { get; } 
        protected Func<Channel<PublicationMessage>, ValueTask> OnChannelDetachedAsyncCached { get; } 

        public IChannelHub<PublicationMessage> ChannelHub { get; }
        public bool OwnsChannelHub { get; }

        public Reproducer(IChannelHub<PublicationMessage> channelHub, bool ownsChannelHub = true)
        {
            ChannelHub = channelHub;
            OwnsChannelHub = ownsChannelHub;
            Reproductions = new ConcurrentDictionary<Symbol, IReproduction>();
            ChannelProcessors = new ConcurrentDictionary<Channel<PublicationMessage>, ReproducerChannelProcessor>();
            
            OnChannelAttachedCached = OnChannelAttached;
            OnChannelDetachedAsyncCached = OnChannelDetachedAsync;
            ChannelHub.Detached += OnChannelDetachedAsyncCached; // Must go first
            ChannelHub.Attached += OnChannelAttachedCached;
        }

        private ValueTask OnChannelDetachedAsync(Channel<PublicationMessage> arg)
        {
            throw new NotImplementedException();
        }

        private void OnChannelAttached(Channel<PublicationMessage> obj)
        {
            throw new NotImplementedException();
        }

        public IReproduction<T> TryAddOrGet<T>(Symbol publicationId, Result<T> initialOutput)
        {
            throw new NotImplementedException();
        }

        public IReproduction TryGet(Symbol publicationId)
        {
            throw new NotImplementedException();
        }

        void IReproducerImpl.OnReproductionDisposed(IReproduction reproduction) 
            => OnReproductionDisposed(reproduction);
        protected virtual void OnReproductionDisposed(IReproduction reproduction)
        {
            if (reproduction.Reproducer != this)
                throw new ArgumentOutOfRangeException(nameof(reproduction));
            Reproductions.TryRemove(reproduction.PublicationId, reproduction);
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            ChannelHub.Attached -= OnChannelAttachedCached; // Must go first
            ChannelHub.Detached -= OnChannelDetachedAsyncCached;
            var channelProcessors = ChannelProcessors;
            while (!channelProcessors.IsEmpty) {
                var tasks = channelProcessors
                    .Take(HardwareInfo.ProcessorCount * 4)
                    .ToList()
                    .Select(p => {
                        var (_, channelProcessor) = (p.Key, p.Value);
                        return channelProcessor.DisposeAsync().AsTask();
                    });
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
        }
    }
}
