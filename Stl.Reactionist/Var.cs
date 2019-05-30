using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Reactionist
{
    public interface IVar : IReadOnlyVar, IMutableResult { }
    public interface IVar<T> : IVar, IReadOnlyVar<T>, IMutableResult<T>
    {
        new Result<T> Result { get; set; }
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

        public Exception Error {
            get => Result.Error;
            set => Result = new Result<T>(default, value);
        }

        public T Value {
            get => Result.Value;
            set => Result = value;
        }
        public object UntypedValue {
            get => Result.UntypedValue;
            set => Result = (T) value;
        }

        public T UnsafeValue => Result.UnsafeValue;
        public object UnsafeUntypedValue => Result.UnsafeUntypedValue;

        public Var(T value = default) => InternalResult = (value, null);
        public Var(Exception error) => InternalResult = (default, error);

        public override string ToString() => Value?.ToString();

        public void ThrowIfError() => Result.ThrowIfError();
        public void Deconstruct(out T value, out Exception error) => Result.Deconstruct(out value, out error);
        
        // Operators

        public static implicit operator T(Var<T> v) => v.Value;
    }

    public static class Var
    {
        public static Var<T> New<T>(T value) => new Var<T>(value);
        public static Var<T> New<T>(Exception error) => new Var<T>(error);
    }
}
