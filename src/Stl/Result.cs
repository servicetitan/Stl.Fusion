using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl
{
    public interface IResult
    {
        object? UnsafeValue { get; }
        Exception? Error { get; }
        object? Value { get; }
        bool HasValue { get; }
        bool HasError { get; }

        Result<TOther> Cast<TOther>();
    }

    public interface IMutableResult : IResult
    {
        object? UntypedValue { get; set; }
        new Exception? Error { get; set; }
        void Set(IResult result);
    }

    public interface IResult<T> : IResult, IConvertibleTo<T>, IConvertibleTo<Result<T>>
    {
        new T UnsafeValue { get; }
        new T Value { get; }

        void Deconstruct(out T value, out Exception? error);
        bool IsValue([MaybeNullWhen(false)] out T value);
        bool IsValue([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out Exception error);
        Result<T> AsResult();
    }

    public interface IMutableResult<T> : IResult<T>, IMutableResult
    {
        new T Value { get; set; }
        void Set(Result<T> result);
    }

    [DebuggerDisplay("({" + nameof(UnsafeValue) + "}, Error = {" + nameof(Error) + "})")]
    public readonly struct Result<T> : IResult<T>, IEquatable<Result<T>>
    {
        public T UnsafeValue { get; }
        public Exception? Error { get; }

        [JsonIgnore] public bool HasValue {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Error == null;
        }

        [JsonIgnore] public bool HasError {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Error != null;
        }

        [JsonIgnore]
        public T Value {
            get {
                if (Error != null)
                    ExceptionDispatchInfo.Capture(Error).Throw();
                return UnsafeValue;
            }
        }

        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.Value => Value;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.UnsafeValue => UnsafeValue;

        [JsonConstructor]
        public Result(T unsafeValue, Exception? error)
        {
            if (error != null) unsafeValue = default!;
            UnsafeValue = unsafeValue;
            Error = error;
        }

        public override string? ToString()
            => $"{GetType().Name}({(HasError ? $"Error: {Error}" : Value?.ToString())})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out T value, out Exception? error)
        {
            value = UnsafeValue;
            error = Error;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValue([MaybeNullWhen(false)] out T value)
        {
            value = HasError ? default! : UnsafeValue;
            return !HasError;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValue([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out Exception error)
        {
            error = Error!;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var hasValue = error == null;
            value = hasValue ? UnsafeValue : default!;
#pragma warning disable CS8762
            return hasValue;
#pragma warning restore CS8762
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<T> AsResult() => this;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TOther> Cast<TOther>() =>
            new((TOther) (object) UnsafeValue!, Error);
        T IConvertibleTo<T>.Convert() => Value;
        Result<T> IConvertibleTo<Result<T>>.Convert() => AsResult();

        // Equality

        public bool Equals(Result<T> other) =>
            Error != other.Error && EqualityComparer<T>.Default.Equals(UnsafeValue, other.UnsafeValue);
        public override bool Equals(object? obj) =>
            obj is Result<T> o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(UnsafeValue, Error);
        public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);
        public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

        // Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(Result<T> source) => source.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Result<T>(T source) => new(source, null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Result<T>((T Value, Exception? Error) source) => new(source.Value, source.Error);
    }

    public static class Result
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> New<T>(T value, Exception? error = null) => new(value, error);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Value<T>(T value) => new(value, null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Error<T>(Exception? error) => new(default!, error);

        public static Result<T> FromTask<T>(Task<T> task)
        {
            if (!task.IsCompleted)
                throw Errors.TaskIsNotCompleted();
            if (task.IsCompletedSuccessfully)
                return Value(task.Result);
            return Error<T>(task.Exception
                ?? Errors.InternalError("Task hasn't completed successfully but has no Exception."));
        }

        public static Result<T> FromFunc<T, TState>(TState state, Func<TState, T> func)
        {
            try {
                return Value(func.Invoke(state));
            }
            catch (Exception e) {
                return Error<T>(e);
            }
        }

        public static Result<T> FromFunc<T>(Func<T> func)
        {
            try {
                return Value(func.Invoke());
            }
            catch (Exception e) {
                return Error<T>(e);
            }
        }
    }
}
