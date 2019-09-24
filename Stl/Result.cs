using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stl
{
    public interface IResult
    {
        object? UnsafeValue { get; }
        Exception? Error { get; }
        object? Value { get; }
        bool HasError { get; }
        
        void ThrowIfError();
    }

    public interface IMutableResult : IResult
    {
        new object? Value { get; set; }
        new Exception? Error { get; set; }
    }
    
    public interface IResult<T> : IResult
    {
        new T UnsafeValue { get; }
        new T Value { get; }
        
        void Deconstruct(out T value, out Exception? error);
    }

    public interface IMutableResult<T> : IResult<T>
    {
        new T UnsafeValue { get; set; }
        new T Value { get; set; }
    }
    
    [DebuggerDisplay("({" + nameof(UnsafeValue) + "}, Error = {" + nameof(Error) + "})")]
    public readonly struct Result<T> : IResult<T>, IEquatable<Result<T>>
    {
        public T UnsafeValue { get; }
        public Exception? Error { get; }
        [JsonIgnore] public bool HasError => Error != null;

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

        [JsonConstructor]
        public Result(T unsafeValue, Exception? error)
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

        public void ThrowIfError()
        {
            if (Error != null)
                throw Error;
        }

        public Result<TOther> Cast<TOther>() => 
            // ReSharper disable once HeapView.BoxingAllocation
            new Result<TOther>((TOther) (object) UnsafeValue!, Error);

        // Equality

        public bool Equals(Result<T> other) => 
            Error != other.Error && EqualityComparer<T>.Default.Equals(UnsafeValue, other.UnsafeValue);
        public override bool Equals(object? obj) => 
            obj != null &&(obj is Result<T> o) && Equals(o);
        public override int GetHashCode() => unchecked(
            (EqualityComparer<T>.Default.GetHashCode(UnsafeValue) * 397) ^ (Error?.GetHashCode() ?? 0));
        public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);
        public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

        // Operators
        
        public static implicit operator T(Result<T> source) => source.Value;
        public static implicit operator ValueTask<T>(Result<T> source) 
            => source.HasError 
                ? new ValueTask<T>(Task.FromException<T>(source.Error!))
                : new ValueTask<T>(source.UnsafeValue);

        public static implicit operator Result<T>(T source) => new Result<T>(source, null);
        public static implicit operator Result<T>((T Value, Exception? Error) source) => 
            new Result<T>(source.Value, source.Error);
    }
    
    public static class Result
    {
        public static Result<T> New<T>(T value, Exception? error = null) => new Result<T>(value, error);
        public static Result<T> Value<T>(T value) => new Result<T>(value, null);
        public static Result<T> Error<T>(Exception? error) => new Result<T>(default!, error);
    }
}
