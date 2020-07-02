using System;
using Newtonsoft.Json;
using Stl.Async;
using Stl.Reflection;

namespace Stl.Internal
{
    public static class Errors
    {
        public static Exception MustBeUnfrozen() =>
            new InvalidOperationException("The object must be unfrozen.");
        public static Exception MustBeUnfrozen(string paramName) =>
            new ArgumentException("The object must be unfrozen.", paramName);
        public static Exception MustBeFrozen() =>
            new InvalidOperationException("The object must be frozen.");
        public static Exception MustBeFrozen(string paramName) =>
            new ArgumentException("The object must be frozen.", paramName);

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

        public static Exception ZListIsTooLong() =>
            new InvalidOperationException("ZList<T> is too long.");
        public static Exception InvalidListFormat() =>
            new FormatException("Invalid list format.");

        public static Exception QueueSizeMustBeGreaterThanZero(string paramName) =>
            new ArgumentOutOfRangeException(paramName, "Queue size must be > 0.");
        public static Exception BufferLengthMustBeGreaterThanOne(string paramName) =>
            new ArgumentOutOfRangeException(paramName, "Buffer length must be > 1.");
        public static Exception BufferLengthMustBeGreaterThanZero(string paramName) =>
            new ArgumentOutOfRangeException(paramName, "Buffer length must be > 0.");
        public static Exception EnqueueCompleted() =>
            new InvalidOperationException("EnqueueCompleted == true.");

        public static Exception CircularDependency<T>(T item) => 
            new InvalidOperationException($"Circular dependency on {item} found.");

        public static Exception CannotActivate(Type type) =>
            new InvalidOperationException($"Cannot find the right constructor to activate type '{type.FullName}'.");

        public static Exception OptionIsNone() =>
            new InvalidOperationException("Option is None.");

        public static Exception TaskIsNotCompleted() =>
            new InvalidOperationException("Task is expected to be completed at this point, but it's not.");

        public static Exception PathIsRelative(string? paramName) =>
            new ArgumentException("Path is relative.", paramName);

        public static Exception UnsupportedTypeForJsonSerialization(Type type)
            => new JsonSerializationException($"Unsupported type: '{type.FullName}'.");

        public static Exception AlreadyDisposed() =>
            new ObjectDisposedException("unknown", "The object is already disposed.");
        public static Exception AlreadyDisposedOrDisposing(DisposalState disposalState = DisposalState.Disposed)
        {
            switch (disposalState) {
            case DisposalState.Disposing:
                return new ObjectDisposedException("unknown", "The object is disposing.");
            case DisposalState.Disposed:
                return new ObjectDisposedException("unknown", "The object is already disposed.");
            default:
                return new InvalidOperationException($"Invalid disposal state: {disposalState}.");
            }
        }

        public static Exception KeyAlreadyExists() =>
            new ArgumentException("Specified key already exists.");
        public static Exception AlreadyInvoked(string methodName) =>
            new InvalidOperationException($"'{methodName}' can be invoked just once.");
        public static Exception AlreadyInitialized() =>
            new InvalidOperationException("Already initialized.");
        public static Exception AlreadyLocked() =>
            new InvalidOperationException($"The lock is already acquired by one of callers of the current method.");
        public static Exception AlreadyUsed() =>
            new InvalidOperationException("The object was already used somewhere else.");
        public static Exception AlreadyCompleted() =>
            new InvalidOperationException("The event source is already completed.");
        public static Exception ThisValueCanBeSetJustOnce() =>
            new InvalidOperationException($"This value can be set just once.");
        public static Exception NoDefaultConstructor(Type type)
            => new InvalidOperationException($"Type '{type.FullName}' doesn't have a default constructor.");

        public static Exception InternalError(string message) =>
            new SystemException(message);
    }
}
