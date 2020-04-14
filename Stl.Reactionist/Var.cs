using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Stl.Reactionist
{
    public interface IVar : IReadOnlyVar, IMutableResult { }
    public interface IVar<T> : IVar, IReadOnlyVar<T>, IMutableResult<T>
    {
        new Result<T> Result { get; set; }
        new T UnsafeValue { get; set; }
        new T Value { get; set; }
    }

    [DebuggerDisplay("{" + nameof(InternalResult) + "}")]
    public class Var<T> : ReactiveWithReactionsBase, IVar<T>
    {
        protected Result<T> InternalResult { get; set; }
        Result<T> IHasInternalResult<T>.InternalResult => InternalResult;

        public Result<T> Result {
            get {
                RegisterDependency();
                return InternalResult;
            }
            set {
                if (EqualityComparer<Result<T>>.Default.Equals(InternalResult, value))
                    return;
                InternalResult = value;
                TriggerReactions(new Event(this, ChangedEventData.Instance));
            }
        }

        public Exception? Error {
            get => Result.Error;
            set => Result = new Result<T>(default!, value);
        }
        public bool HasValue => Result.HasValue;
        public bool HasError => Result.HasError;

        public T UnsafeValue {
            get => Result.UnsafeValue;
            set => Result = value!;
        }

        public T Value {
            get => Result.Value;
            set => Result = value!;
        }

        object? IMutableResult.Value {
            // ReSharper disable once HeapView.BoxingAllocation
            get => Value;
            set => Value = (T) value!;
        }
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.Value => Value;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.UnsafeValue => Result.UnsafeValue;

        public Var(T value = default) => InternalResult = (value, null);
        public Var(Exception error) => InternalResult = (default(T)!, error);

        public override string? ToString() => Value?.ToString();

        public void Deconstruct(out T value, out Exception? error) 
            => Result.Deconstruct(out value, out error);
        public bool IsValue([MaybeNullWhen(false)] out T value) 
            => Result.IsValue(out value);
        public bool IsValue([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out Exception error) 
            => Result.IsValue(out value, out error!);
        public void ThrowIfError() => Result.ThrowIfError();

        // Operators

        public static implicit operator T(Var<T> v) => v.Value;
    }

    public static class Var
    {
        public static Var<T> New<T>(T value) => new Var<T>(value);
        public static Var<T> New<T>(Exception error) => new Var<T>(error);
    }
}
