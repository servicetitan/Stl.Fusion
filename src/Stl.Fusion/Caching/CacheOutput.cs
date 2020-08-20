using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Newtonsoft.Json;

namespace Stl.Fusion.Caching
{
    [DebuggerDisplay("({" + nameof(UnsafeValue) + "}, Error = {" + nameof(Error) + "})")]
    public sealed class ResultBox<T> : IResult<T>
    {
        public static readonly ResultBox<T> Default = new ResultBox<T>(default!, null);

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
                else {
                    // That's the right way to re-throw an exception and preserve its stack trace
                    ExceptionDispatchInfo.Capture(Error).Throw();
                    return default!; // Never executed, but no way to get rid of this
                }
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
        public Result<T> ToResult() => new Result<T>(UnsafeValue, Error);

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

        public void ThrowIfError()
        {
            if (Error != null)
                throw Error;
        }
    }
}
