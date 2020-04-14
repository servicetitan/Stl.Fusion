using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Stl.Reactionist
{
    public interface IHasInternalResult<T>
    {
        Result<T> InternalResult { get; }
    }
    public interface IReadOnlyVar : IReactive, IResult { }
    public interface IReadOnlyVar<T> : IReadOnlyVar, IResult<T>, IHasInternalResult<T>
    {
        Result<T> Result { get; }
    }
    
    [DebuggerDisplay("{" + nameof(InternalResult) + "}")]
    public class ReadOnlyVar<T> : ReactiveWithReactionsBase, IReadOnlyVar<T>
    {
        protected IVar<T> Storage { get; }
        protected Result<T> InternalResult => Storage.InternalResult;
        Result<T> IHasInternalResult<T>.InternalResult => InternalResult;

        public Result<T> Result => Storage.Result;
        public Exception? Error => Storage.Error;
        public bool HasValue => Storage.HasValue;
        public bool HasError => Storage.HasError;
        public T UnsafeValue => Storage.UnsafeValue;
        public T Value => Storage.Value;

        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.Value => Storage.Value;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.UnsafeValue => Storage.UnsafeValue;

        public ReadOnlyVar(IVar<T> storage)
        {
            Storage = storage;
            // Zero allocations here; this is to re-emit the events happening w/ Storage as the own ones
            Storage.AddReaction(new Reaction(this,
                (self, @event) => TriggerReactions(new Event((IReactive?) self, @event.Data))));
        }

        public void Deconstruct(out T value, out Exception? error) 
            => Storage.Deconstruct(out value, out error);
        public bool IsValue([MaybeNullWhen(false)] out T value) 
            => Storage.IsValue(out value);
        public bool IsValue([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out Exception error) 
            => Storage.IsValue(out value, out error!);
        public void ThrowIfError() => Storage.ThrowIfError();

        // Operators

        public static implicit operator T(ReadOnlyVar<T> v) => v.Value;
    }

    public static class ReadOnlyVar
    {
        public static ReadOnlyVar<T> New<T>(IVar<T> storage) => new ReadOnlyVar<T>(storage);
    }
}
