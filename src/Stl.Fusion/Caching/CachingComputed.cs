using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Frozen;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Caching
{
    public interface ICachingComputed : IComputed
    {
        bool IsFromCache { get; }
        IResult? CacheOutput { get; }

        void DropCachedOutput();
        ValueTask<IResult?> GetOutputAsync(CancellationToken cancellationToken = default);
    }

    public interface ICachingComputed<T> : ICachingComputed, IComputed<T>
    {
        new ResultBox<T>? CacheOutput { get; }

        bool TrySetOutput(Result<T> output, bool isFromCache);
        new ValueTask<ResultBox<T>?> GetOutputAsync(CancellationToken cancellationToken = default);
    }

    public class CachingComputed<T> : Computed<T>, ICachingComputed<T>
    {
        private volatile ResultBox<T>? _cacheOutput = null;

        public bool IsFromCache { get; private set; }
        public override Result<T> Output {
            get {
                AssertStateIsNot(ComputedState.Computing);
                var output = _cacheOutput;
                if (output == null)
                    throw Errors.CachedOutputIsAlreadyDropped();
                return output.ToResult();
            }
        }
        IResult? ICachingComputed.CacheOutput => CacheOutput;
        public ResultBox<T>? CacheOutput => _cacheOutput;
        public new ICachingFunction<InterceptedInput, T> Function
            => (ICachingFunction<InterceptedInput, T>) Input.Function;

        public CachingComputed(ComputedOptions options, InterceptedInput input, LTag version)
            : base(options, input, version) { }
        protected CachingComputed(ComputedOptions options, InterceptedInput input, ResultBox<T> cachedOutput, LTag version, bool isConsistent = true)
            : base(options, input, default, version, isConsistent)
            => _cacheOutput = cachedOutput;

        public void DropCachedOutput()
        {
            AssertStateIsNot(ComputedState.Computing);
            Interlocked.Exchange(ref _cacheOutput, null);
        }

        public override bool TrySetOutput(Result<T> output)
            => TrySetOutput(output, false);
        public bool TrySetOutput(Result<T> output, bool isFromCache)
        {
            if (output.IsValue(out var v) && v is IFrozen f)
                f.Freeze();
            if (State != ComputedState.Computing)
                return false;
            lock (Lock) {
                if (State != ComputedState.Computing)
                    return false;
                SetStateUnsafe(ComputedState.Consistent);
                Interlocked.Exchange(ref _cacheOutput, new ResultBox<T>(output));
                IsFromCache = isFromCache;
            }
            OnOutputSet(output);
            return true;
        }

        async ValueTask<IResult?> ICachingComputed.GetOutputAsync(CancellationToken cancellationToken)
            => await GetOutputAsync(cancellationToken).ConfigureAwait(false);
        public async ValueTask<ResultBox<T>?> GetOutputAsync(CancellationToken cancellationToken)
        {
            var cachedOutput = CacheOutput;
            if (cachedOutput != null)
                return cachedOutput;

            Option<Result<T>> outputOpt;
            using (Computed.Suppress()) {
                var fn = (ICachingFunction<InterceptedInput, T>) Input.Function;
                outputOpt = await fn.GetCachedOutputAsync(Input, cancellationToken)
                    .ConfigureAwait(false);
            }
            if (!outputOpt.IsSome(out var output)) {
                Invalidate();
                return null;
            }

            cachedOutput = new ResultBox<T>(output);
            Interlocked.Exchange(ref _cacheOutput, cachedOutput);
            return cachedOutput;
        }

        protected override void OnInvalidated()
        {
            base.OnInvalidated();
            if (!IsFromCache) {
                // We created this cache entry, so we have to remove it
                Task.Run(() => Function.RemoveCachedOutputAsync(Input));
            }
        }
    }
}
