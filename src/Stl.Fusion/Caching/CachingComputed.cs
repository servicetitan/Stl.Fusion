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
        IResult? MaybeOutput { get; }

        void ReleaseOutput();
        ValueTask<IResult?> GetOutputAsync(CancellationToken cancellationToken = default);
    }

    public interface ICachingComputed<T> : ICachingComputed, IComputed<T>
    {
        new ResultBox<T>? MaybeOutput { get; }

        bool TrySetOutput(Result<T> output, bool isFromCache);
        new ValueTask<ResultBox<T>?> GetOutputAsync(CancellationToken cancellationToken = default);
    }

    public class CachingComputed<T> : Computed<T>, ICachingComputed<T>
    {
        private volatile ResultBox<T>? _maybeOutput = null;

        public bool IsFromCache { get; private set; }
        public override Result<T> Output {
            get {
                AssertStateIsNot(ComputedState.Computing);
                var output = _maybeOutput;
                if (output == null)
                    throw Errors.OutputIsAlreadyReleased();
                return output.ToResult();
            }
        }
        IResult? ICachingComputed.MaybeOutput => MaybeOutput;
        public ResultBox<T>? MaybeOutput => _maybeOutput;
        public new ICachingFunction<InterceptedInput, T> Function
            => (ICachingFunction<InterceptedInput, T>) Input.Function;

        public CachingComputed(ComputedOptions options, InterceptedInput input, LTag version)
            : base(options, input, version) { }
        protected CachingComputed(ComputedOptions options, InterceptedInput input, ResultBox<T> maybeOutput, LTag version, bool isConsistent = true)
            : base(options, input, default, version, isConsistent)
            => _maybeOutput = maybeOutput;

        public void ReleaseOutput()
        {
            AssertStateIsNot(ComputedState.Computing);
            Interlocked.Exchange(ref _maybeOutput, null);
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
                Interlocked.Exchange(ref _maybeOutput, new ResultBox<T>(output));
                IsFromCache = isFromCache;
            }
            OnOutputSet(output);
            return true;
        }

        async ValueTask<IResult?> ICachingComputed.GetOutputAsync(CancellationToken cancellationToken)
            => await GetOutputAsync(cancellationToken).ConfigureAwait(false);
        public async ValueTask<ResultBox<T>?> GetOutputAsync(CancellationToken cancellationToken)
        {
            var maybeOutput = MaybeOutput;
            if (maybeOutput != null)
                return maybeOutput;

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

            maybeOutput = new ResultBox<T>(output);
            Interlocked.Exchange(ref _maybeOutput, maybeOutput);
            return maybeOutput;
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
