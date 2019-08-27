using System;
using Stl.Async;
using Stl.Reflection;

namespace Stl.Internal
{
    public static class Errors
    {
        public static Exception InvokerIsAlreadyRunning() =>
            new InvalidOperationException("Can't perform this action while invocation is already in progress.");

        public static Exception MissingCliArgument(string template) =>
            new ArgumentException($"Required argument with template '{template ?? "(unknown)"}' is missing.");
        public static Exception UnsupportedFormatString(string format) =>
            new ArgumentException("Unsupported format string: '{0}'.");

        public static Exception ExpressionDoesNotSpecifyAMember(string expression) =>
            new ArgumentException("Expression '{expression}' does not specify a member.");
        public static Exception UnexpectedMemberType(string memberType) =>
            new InvalidOperationException($"Unexpected member type: {memberType}");

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
        public static Exception CircularDependency<T>(T item) => 
            new InvalidOperationException($"Circular dependency on {item} found.");

        public static Exception AlreadyInvoked(string methodName) =>
            new InvalidOperationException($"'{methodName}' can be invoked just once.");
        public static Exception AlreadyInitialized() =>
            new InvalidOperationException("Already initialized.");
        public static Exception AlreadyLocked() =>
            new InvalidOperationException($"The lock is already acquired by one of callers of the current method.");
        public static Exception ThisValueCanBeSetJustOnce() =>
            new InvalidOperationException($"This value can be set just once.");

        public static Exception CannotActivate(Type type) =>
            new InvalidOperationException($"Can't find the constructor to activate type '{type.Name}'.");
    }
}
