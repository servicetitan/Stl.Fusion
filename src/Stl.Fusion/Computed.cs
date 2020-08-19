using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Collections;
using Stl.Collections.Slim;
using Stl.Frozen;
using Stl.Fusion.Caching;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;

namespace Stl.Fusion
{
    public enum ComputedState
    {
        Computing = 0,
        Consistent,
        Invalidated,
    }

    public interface IComputed : IResult
    {
        ComputedOptions Options { get; set; }
        ComputedInput Input { get; }
        IResult Output { get; }
        IResult? MaybeOutput { get; }
        Type OutputType { get; }
        LTag Version { get; } // ~ Unique for the specific (Func, Key) pair
        ComputedState State { get; }
        bool IsConsistent { get; }
        event Action<IComputed> Invalidated;

        bool Invalidate();
        void ReleaseOutput();
        TResult Apply<TArg, TResult>(IComputedApplyHandler<TArg, TResult> handler, TArg arg);

        ValueTask<IResult?> GetOutputAsync(CancellationToken cancellationToken = default);
        ValueTask<IComputed> UpdateAsync(bool addDependency, CancellationToken cancellationToken = default);
        ValueTask<object> UseAsync(CancellationToken cancellationToken = default);
    }

    public interface IComputed<TOut> : IComputed, IResult<TOut>
    {
        new Result<TOut> Output { get; }
        new Option<Result<TOut>> MaybeOutput { get; }
        bool TrySetOutput(Result<TOut> output);
        void SetOutput(Result<TOut> output);

        new ValueTask<Option<Result<TOut>>> GetOutputAsync(CancellationToken cancellationToken = default);
        new ValueTask<IComputed<TOut>> UpdateAsync(bool addDependency, CancellationToken cancellationToken = default);
        new ValueTask<TOut> UseAsync(CancellationToken cancellationToken = default);
    }

    public interface IComputedWithTypedInput<out TIn> : IComputed
        where TIn : ComputedInput
    {
        new TIn Input { get; }
    }

    public interface IComputed<out TIn, TOut> : IComputed<TOut>, IComputedWithTypedInput<TIn>
        where TIn : ComputedInput
    { }

    public class Computed<TIn, TOut> : IComputed<TIn, TOut>, IComputedImpl
        where TIn : ComputedInput
    {
        private class OutputState
        {
            public readonly Option<Result<TOut>> MaybeOutput;
            public OutputState(Option<Result<TOut>> maybeOutput) => MaybeOutput = maybeOutput;
        }

        private ComputedOptions _options;
        private volatile int _state;
        private volatile OutputState? _outputState = null;
        private Result<TOut> _output;
        private RefHashSetSlim2<IComputedImpl> _used = default;
        private HashSetSlim2<(ComputedInput Input, LTag Version)> _usedBy = default;
        // ReSharper disable once InconsistentNaming
        private event Action<IComputed>? _invalidated;
        private bool _invalidateOnSetOutput;
        private object Lock => this;

        public ComputedOptions Options {
            get => _options;
            set {
                AssertStateIs(ComputedState.Computing);
                _options = value;
            }
        }

        public TIn Input { get; }
        public ComputedState State => (ComputedState) _state;
        public bool IsConsistent => State == ComputedState.Consistent;
        public IFunction<TIn, TOut> Function => (IFunction<TIn, TOut>) Input.Function;
        public LTag Version { get; }

        public Type OutputType => typeof(TOut);
        public Result<TOut> Output {
            get {
                AssertStateIsNot(ComputedState.Computing);
                return MaybeOutput.Value;
            }
        }
        public Option<Result<TOut>> MaybeOutput {
            get {
                // Concurrency: _outputState should be set before resetting the _output!
                var output = _output;
                return _outputState?.MaybeOutput ?? output;
            }
        }

        // IResult<T> properties
        public Exception? Error => Output.Error;
        public bool HasValue => Output.HasValue;
        public bool HasError => Output.HasError;
        public TOut UnsafeValue => Output.UnsafeValue;
        public TOut Value => Output.Value;

        // "Untyped" versions of properties
        ComputedInput IComputed.Input => Input;
        // ReSharper disable once HeapView.BoxingAllocation
        IResult IComputed.Output => Output;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.UnsafeValue => Output.UnsafeValue;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.Value => Output.Value;
        IResult? IComputed.MaybeOutput => MaybeOutput.IsSome(out var r) ? (IResult) r : null;

        public event Action<IComputed> Invalidated {
            add {
                if (State == ComputedState.Invalidated) {
                    value?.Invoke(this);
                    return;
                }
                lock (Lock) {
                    if (State == ComputedState.Invalidated) {
                        value?.Invoke(this);
                        return;
                    }
                    _invalidated += value;
                }
            }
            remove => _invalidated -= value;
        }

        public Computed(ComputedOptions options, TIn input, LTag version)
        {
            _options = options;
            Input = input;
            Version = version;
            ComputedRegistry.Instance.Register(this);
        }

        public Computed(ComputedOptions options, TIn input, Result<TOut> output, LTag version, bool isConsistent = true)
        {
            if (output.IsValue(out var v) && v is IFrozen f)
                f.Freeze();
            _options = options;
            Input = input;
            _state = (int) (isConsistent ? ComputedState.Consistent : ComputedState.Invalidated);
            _output = output;
            Version = version;
            if (isConsistent)
                ComputedRegistry.Instance.Register(this);
        }

        public override string ToString()
            => $"{GetType().Name}({Input} {Version}, State: {State})";

        void IComputedImpl.AddUsed(IComputedImpl used)
        {
            // Debug.WriteLine($"{nameof(IComputedImpl.AddUsed)}: {this} <- {used}");
            lock (Lock) {
                switch (State) {
                case ComputedState.Consistent:
                    throw Errors.WrongComputedState(State);
                case ComputedState.Invalidated:
                    return; // Already invalidated, so nothing to do here
                }
                used.AddUsedBy(this);
                _used.Add(used);
            }
        }

        void IComputedImpl.AddUsedBy(IComputedImpl usedBy)
        {
            lock (Lock) {
                switch (State) {
                case ComputedState.Computing:
                    throw Errors.WrongComputedState(State);
                case ComputedState.Invalidated:
                    usedBy.Invalidate();
                    return;
                }

                // The invalidation could happen here -
                // that's why there is a second check later
                // in this method
                var usedByRef = (usedBy.Input, usedBy.Version);
                _usedBy.Add(usedByRef);

                // Second check
                if (State == ComputedState.Invalidated) {
                    _usedBy.Remove(usedByRef);
                    usedBy.Invalidate();
                }
            }
        }

        void IComputedImpl.RemoveUsedBy(IComputedImpl usedBy)
        {
            lock (Lock) {
                _usedBy.Remove((usedBy.Input, usedBy.Version));
            }
        }

        public bool TrySetOutput(Result<TOut> output)
        {
            if (output.IsValue(out var v) && v is IFrozen f)
                f.Freeze();
            if (State != ComputedState.Computing)
                return false;
            lock (Lock) {
                if (State != ComputedState.Computing)
                    return false;
                SetStateUnsafe(ComputedState.Consistent);
                _output = output;
            }
            if (_invalidateOnSetOutput)
                Invalidate();
            else {
                var timeout = output.HasError
                    ? _options.ErrorAutoInvalidateTime
                    : _options.AutoInvalidateTime;
                if (timeout != TimeSpan.MaxValue)
                    AutoInvalidate(timeout);
            }
            return true;
        }

        public void SetOutput(Result<TOut> output)
        {
            if (!TrySetOutput(output))
                throw Errors.WrongComputedState(ComputedState.Computing, State);
        }

        public bool Invalidate()
        {
            if (State == ComputedState.Invalidated)
                return false;
            // Debug.WriteLine($"{nameof(Invalidate)}: {this}");
            MemoryBuffer<(ComputedInput Input, LTag Version)> usedBy = default;
            var invalidateOnSetOutput = false;
            try {
                lock (Lock) {
                    switch (State) {
                    case ComputedState.Invalidated:
                        return false;
                    case ComputedState.Computing:
                        invalidateOnSetOutput = true;
                        return true;
                    }
                    SetStateUnsafe(ComputedState.Invalidated);
                    usedBy = MemoryBuffer<(ComputedInput, LTag)>.LeaseAndSetCount(_usedBy.Count);
                    _usedBy.CopyTo(usedBy.Span);
                    _usedBy.Clear();
                    _used.Apply(this, (self, c) => c.RemoveUsedBy(self));
                    _used.Clear();
                }
                try {
                    _invalidated?.Invoke(this);
                }
                catch {
                    // We should never throw errors during the invalidation
                }
                var computedRegistry = ComputedRegistry.Instance;
                for (var i = 0; i < usedBy.Span.Length; i++) {
                    ref var d = ref usedBy.Span[i];
                    var c = computedRegistry.TryGet(d.Input);
                    if (c != null && c.Version == d.Version)
                        c.Invalidate();
                    else
                        Debugger.Break();
                    // Just in case buffers aren't cleaned up when you return them back
                    d = default!;
                }
                return true;
            }
            finally {
                usedBy.Release();
                if (invalidateOnSetOutput)
                    _invalidateOnSetOutput = true;
                else
                    OnInvalidated();
            }
        }

        protected virtual void OnInvalidated()
        {
            ComputedRegistry.Instance.Unregister(this);
            this.CancelKeepAlive();
        }

        // Caching-related

        public void ReleaseOutput()
        {
            if (!Options.IsCachingEnabled)
                throw Errors.UnsupportedRequiresCaching();
            AssertStateIsNot(ComputedState.Computing);
            // Concurrency: _outputState should be set before resetting the _output!
            Interlocked.Exchange(ref _outputState, new OutputState(Option<Result<TOut>>.None));
            _output = default;
        }

        async ValueTask<IResult?> IComputed.GetOutputAsync(CancellationToken cancellationToken)
        {
            var outputOpt = await GetOutputAsync(cancellationToken).ConfigureAwait(false);
            return outputOpt.IsSome(out var r) ? (IResult) r : null;
        }

        public async ValueTask<Option<Result<TOut>>> GetOutputAsync(CancellationToken cancellationToken)
        {
            var outputOpt = MaybeOutput;
            if (outputOpt.HasValue)
                return outputOpt;

            Option<Result<TOut>> maybeOutput;
            using (Computed.Suppress()) {
                var fn = (ICachingFunction<TIn, TOut>) Input.Function;
                maybeOutput = await fn.GetCachedOutputAsync(Input, cancellationToken)
                    .ConfigureAwait(false);
            }
            if (!maybeOutput.IsSome(out var output)) {
                Invalidate();
                return Option<Result<TOut>>.None;
            }
            Interlocked.Exchange(ref _outputState, new OutputState(output));
            return output;
        }

        // UpdateAsync

        async ValueTask<IComputed> IComputed.UpdateAsync(bool addDependency, CancellationToken cancellationToken)
            => await UpdateAsync(addDependency, cancellationToken).ConfigureAwait(false);
        public async ValueTask<IComputed<TOut>> UpdateAsync(bool addDependency, CancellationToken cancellationToken = default)
        {
            var usedBy = addDependency ? Computed.GetCurrent() : null;
            var context = ComputeContext.Current;

            if (this.TryUseExisting(context, usedBy))
                return this;
            return await Function.InvokeAsync(Input, usedBy, context, cancellationToken);
        }

        // UseAsync

        async ValueTask<object> IComputed.UseAsync(CancellationToken cancellationToken)
            => (await UseAsync(cancellationToken).ConfigureAwait(false))!;
        public async ValueTask<TOut> UseAsync(CancellationToken cancellationToken = default)
        {
            var computed = await UpdateAsync(true, cancellationToken).ConfigureAwait(false);
            return computed.Value;
        }

        // Apply

        public TResult Apply<TArg, TResult>(IComputedApplyHandler<TArg, TResult> handler, TArg arg)
            => handler.Apply(this, arg);

        // IResult<T> methods

        public void Deconstruct(out TOut value, out Exception? error)
            => Output.Deconstruct(out value, out error);
        public bool IsValue([MaybeNullWhen(false)] out TOut value)
            => Output.IsValue(out value);
        public bool IsValue([MaybeNullWhen(false)] out TOut value, [MaybeNullWhen(true)] out Exception error)
            => Output.IsValue(out value, out error!);
        public void ThrowIfError() => Output.ThrowIfError();

        // Protected & private methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetStateUnsafe(ComputedState newState)
            => _state = (int) newState;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AssertStateIs(ComputedState expectedState)
        {
            if (State != expectedState)
                throw Errors.WrongComputedState(expectedState, State);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AssertStateIsNot(ComputedState unexpectedState)
        {
            if (State == unexpectedState)
                throw Errors.WrongComputedState(State);
        }

        private void AutoInvalidate(TimeSpan timeout)
        {
            // This method is called just once for sure
            var cts = new CancellationTokenSource(timeout);
            Invalidated += _ => {
                try {
                    if (!cts.IsCancellationRequested)
                        cts.Cancel(true);
                } catch {
                    // Intended: this method should never throw any exceptions
                }
            };
            cts.Token.Register(() => {
                cts.Dispose();
                // No need to schedule this via Task.Run, since this code is
                // either invoked from Invalidate method (via Invalidated handler),
                // so Invalidate() call will do nothing & return immediately,
                // or it's invoked via one of timer threads, i.e. where it's
                // totally fine to invoke Invalidate directly as well.
                Invalidate();
            }, false);
        }
    }

    public class Computed<T> : Computed<InterceptedInput, T>
    {
        public Computed(ComputedOptions options, InterceptedInput input, LTag version)
            : base(options, input, version) { }
        public Computed(ComputedOptions options, InterceptedInput input, Result<T> output, LTag version, bool isConsistent = true)
            : base(options, input, output, version, isConsistent) { }
    }
}
