using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization;

namespace Stl
{
    /// <summary>
    /// A class describing strongly typed result of a computation.
    /// Complements <see cref="Result{T}"/> (struct).
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Value"/>.</typeparam>
    [DebuggerDisplay("({" + nameof(ValueOrDefault) + "}, Error = {" + nameof(Error) + "})")]
    public sealed class ResultBox<T> : IResult<T>
    {
        public static readonly ResultBox<T> Default = new(default!, null);

        /// <inheritdoc />
        [MaybeNull]
        public T ValueOrDefault { get; }
        /// <inheritdoc />
        public Exception? Error { get; }

        /// <inheritdoc />
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public bool HasValue {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Error == null;
        }

        /// <inheritdoc />
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public bool HasError {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Error != null;
        }

        /// <inheritdoc />
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public T Value {
            get {
                if (Error == null)
                    return ValueOrDefault!;
                // That's the right way to re-throw an exception and preserve its stack trace
                ExceptionDispatchInfo.Capture(Error).Throw();
                return default!; // Never executed, but no way to get rid of this
            }
        }

        /// <inheritdoc />
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.Value => Value;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="result">A result to copy the properties from.</param>
        public ResultBox(Result<T> result)
            : this(result.ValueOrDefault!, result.Error) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="valueOrDefault"><see cref="ValueOrDefault"/> value.</param>
        /// <param name="error"><see cref="Error"/> value.</param>
        [JsonConstructor, Newtonsoft.Json.JsonConstructor]
        public ResultBox(T valueOrDefault, Exception? error)
        {
            if (error != null) valueOrDefault = default!;
            ValueOrDefault = valueOrDefault;
            Error = error;
        }

        /// <inheritdoc />
        public override string? ToString() => Value?.ToString();

        /// <inheritdoc />
        public void Deconstruct(out T value, out Exception? error)
        {
            value = ValueOrDefault!;
            error = Error;
        }

        /// <inheritdoc />
        public bool IsValue([MaybeNullWhen(false)] out T value)
        {
            value = HasError ? default! : ValueOrDefault!;
            return !HasError;
        }

        /// <inheritdoc />
        public bool IsValue([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out Exception error)
        {
            error = Error!;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var hasValue = error == null;
            value = hasValue ? ValueOrDefault! : default!;
            return hasValue;
        }

        /// <inheritdoc />
        public Result<T> AsResult()
            => new(ValueOrDefault!, Error);
        /// <inheritdoc />
        public Result<TOther> Cast<TOther>()
            => new((TOther) (object) ValueOrDefault!, Error);
        /// <inheritdoc />
        T IConvertibleTo<T>.Convert() => Value;
        /// <inheritdoc />
        Result<T> IConvertibleTo<Result<T>>.Convert() => AsResult();

        // Operators

        public static implicit operator T(ResultBox<T> source) => source.Value;
        public static implicit operator Result<T>(ResultBox<T> source) => source.AsResult();
        public static implicit operator ResultBox<T>(Result<T> source) => new(source);
        public static implicit operator ResultBox<T>(T source) => new(source, null);
        public static implicit operator ResultBox<T>((T Value, Exception? Error) source)
            => new(source.Value, source.Error);
    }

    public static class ResultBox
    {
        public static ResultBox<T> New<T>(Result<T> result) => new(result);
        public static ResultBox<T> New<T>(T value, Exception? error = null) => new(value, error);
        public static ResultBox<T> Value<T>(T value) => new(value, null);
        public static ResultBox<T> Error<T>(Exception? error) => new(default!, error);
    }
}
