using System;
using System.Diagnostics.CodeAnalysis;

namespace Stl.Fusion.UI
{
    public interface IState : IResult
    {
        IComputed Computed { get; }
        event Action<IState>? Invalidated;
        event Action<IState>? Updated;
    }

    public interface IState<T> : IState, IResult<T>
    {
        new IComputed<T> Computed { get; }
        new event Action<IState<T>>? Invalidated;
        new event Action<IState<T>>? Updated;
    }

    public abstract class State<T> : IState<T>
    {
        public abstract IComputed<T> Computed { get; }
        public T UnsafeValue => Computed.UnsafeValue;
        public T Value => Computed.Value;
        public Exception? Error => Computed.Error;
        public bool HasValue => Computed.HasValue;
        public bool HasError => Computed.HasError;
        IComputed IState.Computed => Computed;
        object? IResult.UnsafeValue => Computed.UnsafeValue;
        object? IResult.Value => Computed.Value;

        event Action<IState>? IState.Invalidated {
            add => Invalidated += value;
            remove => Invalidated -= value;
        }
        event Action<IState>? IState.Updated {
            add => Updated += value;
            remove => Updated -= value;
        }
        public event Action<IState<T>>? Invalidated;
        public event Action<IState<T>>? Updated;

        public void Deconstruct(out T value, out Exception? error)
            => Computed.Deconstruct(out value, out error);

        public bool IsValue(out T value)
            => Computed.IsValue(out value!);
        public bool IsValue([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out Exception error)
            => Computed.IsValue(out value, out error);

        public Result<T> AsResult()
            => Computed.AsResult();
        public Result<TOther> AsResult<TOther>()
            => Computed.AsResult<TOther>();

        public void ThrowIfError()
            => Computed.ThrowIfError();

        protected virtual void OnInvalidated()
            => Invalidated?.Invoke(this);
        protected virtual void OnUpdated()
            => Updated?.Invoke(this);
    }
}
