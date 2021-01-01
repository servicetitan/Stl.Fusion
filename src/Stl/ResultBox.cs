using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Newtonsoft.Json;
using Stl.Extensibility;

namespace Stl
{
    [DebuggerDisplay("({" + nameof(UnsafeValue) + "}, Error = {" + nameof(Error) + "})")]
    public sealed class ResultBox<T> : IResult<T>
    {
        public static readonly ResultBox<T> Default = new(default!, null);

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
                if (Error == null)
                    return UnsafeValue;
                // That's the right way to re-throw an exception and preserve its stack trace
                ExceptionDispatchInfo.Capture(Error).Throw();
                return default!; // Never executed, but no way to get rid of this
            }
        }

        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.Value => Value;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.UnsafeValue => UnsafeValue;

        public ResultBox(Result<T> result)
            : this(result.UnsafeValue, result.Error) { }
        [JsonConstructor]
        public ResultBox(T unsafeValue, Exception? error)
        {
            if (error != null) unsafeValue = default!;
            UnsafeValue = unsafeValue;
            Error = error;
        }

        public override string? ToString() => Value?.ToString();

        public void Deconstruct(out T value, out Exception? error)
        {
            value = UnsafeValue;
            error = Error;
        }

        public bool IsValue([MaybeNullWhen(false)] out T value)
        {
            value = HasError ? default! : UnsafeValue;
            return !HasError;
        }

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

        public Result<T> AsResult()
            => new(UnsafeValue, Error);
        public Result<TOther> Cast<TOther>()
            => new((TOther) (object) UnsafeValue!, Error);
        T IConvertibleTo<T>.Convert() => Value;
        Result<T> IConvertibleTo<Result<T>>.Convert() => AsResult();

        // Operators

        public static implicit operator T(ResultBox<T> source) => source.Value;
        public static implicit operator Result<T>(ResultBox<T> source) => source.AsResult();
        public static implicit operator ResultBox<T>(Result<T> source) => new(source);
        public static implicit operator ResultBox<T>(T source) => new(source, null);
        public static implicit operator ResultBox<T>((T Value, Exception? Error) source) =>
            new ResultBox<T>(source.Value, source.Error);
    }

    public static class ResultBox
    {
        public static ResultBox<T> New<T>(Result<T> result) => new(result);
        public static ResultBox<T> New<T>(T value, Exception? error = null) => new(value, error);
        public static ResultBox<T> Value<T>(T value) => new(value, null);
        public static ResultBox<T> Error<T>(Exception? error) => new(default!, error);
    }
}
