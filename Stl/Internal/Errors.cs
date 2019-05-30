using System;
using Stl.Async;

namespace Stl.Internal
{
    public static class Errors
    {
        public static Exception PromiseIsAlreadyCompleted() =>
            new InvalidOperationException("Promise is already completed.");
        
        public static Exception ResponseIsAlreadyCreated(Type expected, Type actual) =>
            new InvalidOperationException(
                $"Can't create response of type '{expected.Name}': response of type '{actual.Name}' is already created.");

        public static Exception QueueSizeMustBeGreaterThanZero(string paramName) =>
            new ArgumentOutOfRangeException(paramName, "Queue size must be > 0.");
        public static Exception BufferLengthMustBeGreaterThanOne(string paramName) =>
            new ArgumentOutOfRangeException(paramName, "Buffer length must be > 1.");
        public static Exception BufferLengthMustBeGreaterThanZero(string paramName) =>
            new ArgumentOutOfRangeException(paramName, "Buffer length must be > 0.");
        public static Exception EnqueueCompleted() =>
            new InvalidOperationException("EnqueueCompleted == true.");

        public static Exception ObjectDisposed() =>
            new ObjectDisposedException(null, "The object is already disposed.");
        public static Exception ObjectDisposedOrDisposing(DisposalState disposalState = DisposalState.Disposed)
        {
            switch (disposalState) {
            case DisposalState.Disposing:
                return new ObjectDisposedException(null, "The object is disposing.");
            case DisposalState.Disposed:
                return new ObjectDisposedException(null, "The object is already disposed.");
            default:
                return new InvalidOperationException($"Invalid disposal state: {disposalState}.");
            }
        }

        public static Exception AlreadyInitialized() =>
            new InvalidOperationException("Already initialized.");
        public static Exception ThisValueCanBeSetJustOnce() =>
            new InvalidOperationException($"This value can be set just once.");
        public static Exception CircularDependency<T>(T item) => 
            new InvalidOperationException($"Circular dependency on {item} found.");
    }
}
