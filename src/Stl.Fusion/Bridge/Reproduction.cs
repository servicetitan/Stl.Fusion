using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public interface IReproduction : IAsyncDisposable
    {
        IReproducer Reproducer { get; }
        Symbol PublisherId { get; }
        Symbol PublicationId { get; }
        IComputed Computed { get; }
        Task<IComputed> NextUpdateTask { get; }
    }

    public interface IReproduction<T> : IReproduction
    {
        new IComputed<T> Computed { get; }
    }

    public interface IReproductionImpl : IReproduction, IFunction { }
    public interface IReproductionImpl<T> : IReproduction<T>, IFunction<ReproductionInput, T>, IReproductionImpl
    {
        void Update(TaggedResult<T> taggerOutput);
    } 

    public class Reproduction<T> : AsyncDisposableBase, IReproductionImpl<T>
    {
        private volatile IComputed<T> _computed;
        protected volatile TaskCompletionSource<IComputed> NextUpdateTcs;
        protected readonly ReproductionInput Input;

        public IReproducer Reproducer { get; }
        public Symbol PublisherId { get; }
        public Symbol PublicationId { get; }
        public IComputed<T> Computed => _computed;
        IComputed IReproduction.Computed => _computed;
        public Task<IComputed> NextUpdateTask => NextUpdateTcs.Task;
        protected object Lock => this;

        public Reproduction(IReproducer reproducer, Symbol publisherId, Symbol publicationId, 
            TaggedResult<T> initialOutput)
        {
            Reproducer = reproducer;
            PublisherId = publisherId;
            PublicationId = publicationId;
            Input = new ReproductionInput(this);
            _computed = new Computed<ReproductionInput, T>(Input, initialOutput.Result, initialOutput.Tag);
        }

        public bool Update(IComputed origin, TaggedResult<T> newOutput)
        {
            var typedOrigin = (Computed<ReproductionInput, T>) origin;
            var spinWait = new SpinWait();
            while (_computed == typedOrigin) {
                var newComputed = new Computed<ReproductionInput, T>(Input, newOutput.Result, newOutput.Tag);
                if (typedOrigin == Interlocked.CompareExchange(ref _computed, newComputed, typedOrigin)) {
                    var nextUpdateTcs = Interlocked.Exchange(ref NextUpdateTcs, new TaskCompletionSource<IComputed>());
                    try {
                        typedOrigin.Invalidate();
                    }
                    finally {
                        nextUpdateTcs.SetResult(newComputed);
                    }
                    return true;
                }
                spinWait.SpinOnce();
            }
            return false;
        }

        #region Explicit impl. of IFunction & IFunction<...>

        async Task<IComputed?> IFunction.InvokeAsync(ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => await InvokeAsync((ReproductionInput) input, usedBy, context, cancellationToken);

        Task IFunction.InvokeAndStripAsync(ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => InvokeAndStripAsync((ReproductionInput) input, usedBy, context, cancellationToken);

        IComputed? IFunction.TryGetCached(ComputedInput input, IComputed? usedBy) 
            => TryGetCached((ReproductionInput) input, usedBy);

        Task<IComputed<T>?> IFunction<ReproductionInput, T>.InvokeAsync(ReproductionInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => InvokeAsync(input, usedBy, context, cancellationToken);

        Task<T> IFunction<ReproductionInput, T>.InvokeAndStripAsync(ReproductionInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => InvokeAndStripAsync(input, usedBy, context, cancellationToken);

        IComputed<T>? IFunction<ReproductionInput, T>.TryGetCached(ReproductionInput input, IComputed? usedBy) 
            => TryGetCached(input, usedBy);

        #endregion

        protected Task<IComputed<T>?> InvokeAsync(ReproductionInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            var reproductionImpl = (IReproductionImpl<T>) input.ReproductionImpl; 
            var computed = reproductionImpl.Computed;
            if (computed.IsConsistent)
                return Task.FromResult(computed)!;
            return NextUpdateTask.ContinueWith((_, arg) => {
                var reproductionImpl1 = (IReproductionImpl<T>) arg;
                return reproductionImpl1.Computed;
            }, reproductionImpl, cancellationToken)!;
        }

        protected async Task<T> InvokeAndStripAsync(ReproductionInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            var reproductionImpl = (IReproductionImpl<T>) input.ReproductionImpl; 
            var computed = reproductionImpl.Computed;
            if (computed.IsConsistent)
                return computed.Value;
            await NextUpdateTask;
            return reproductionImpl.Computed.Value;
        }

        protected IComputed<T>? TryGetCached(ReproductionInput input, IComputed? usedBy)
        {
            var reproductionImpl = (IReproductionImpl<T>) input.ReproductionImpl;
            return reproductionImpl.Computed;
        }

        protected override ValueTask DisposeInternalAsync(bool disposing)
        {
            ((IReproducerImpl) Reproducer).OnReproductionDisposed(this);
            return base.DisposeInternalAsync(disposing);
        }
    }
}
