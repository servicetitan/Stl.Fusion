using System.Security;

namespace Stl.Internal;

public static class Errors
{
    public static Exception MustImplement<TExpected>(Type type, string? argumentName = null)
        => MustImplement(type, typeof(TExpected), argumentName);
    public static Exception MustImplement(Type type, Type expectedType, string? argumentName = null)
    {
        var message = $"'{type}' must implement '{expectedType}'.";
        return argumentName.IsNullOrEmpty()
            ? new InvalidOperationException(message)
            : new ArgumentOutOfRangeException(argumentName, message);
    }

    public static Exception MustBeAssignableTo<TExpected>(Type type, string? argumentName = null)
        => MustBeAssignableTo(type, typeof(TExpected), argumentName);
    public static Exception MustBeAssignableTo(Type type, Type mustBeAssignableToType, string? argumentName = null)
    {
        var message = $"'{type}' must be assignable to '{mustBeAssignableToType}'.";
        return argumentName.IsNullOrEmpty()
            ? new InvalidOperationException(message)
            : new ArgumentOutOfRangeException(argumentName, message);
    }

    public static Exception ExpressionDoesNotSpecifyAMember(string expression)
        => new ArgumentException($"Expression '{expression}' does not specify a member.");
    public static Exception UnexpectedMemberType(string memberType)
        => new InvalidOperationException($"Unexpected member type: {memberType}");

    public static Exception InvalidListFormat()
        => new FormatException("Invalid list format.");

    public static Exception CircularDependency<T>(T item)
        => new InvalidOperationException($"Circular dependency on {item} found.");

    public static Exception OptionIsNone()
        => new InvalidOperationException("Option is None.");

    public static Exception TaskIsNotCompleted()
        => new InvalidOperationException("Task is expected to be completed at this point, but it's not.");
    public static Exception TaskIsFaultedButNoExceptionAvailable()
        => new InvalidOperationException("Task hasn't completed successfully but has no Exception.");

    public static Exception PathIsRelative(string? paramName)
        => new ArgumentException("Path is relative.", paramName);

    public static Exception WrongExceptionType(Type type)
        => new SecurityException($"Wrong exception type: '{type}'.");

    public static Exception AlreadyDisposed()
        => new ObjectDisposedException("unknown", "The object is already disposed.");
    public static Exception AlreadyDisposedOrDisposing()
        => new ObjectDisposedException("unknown", "The object is already disposed or disposing.");
    public static Exception AlreadyStopped()
        => new InvalidOperationException("The process or task is already stopped.");

    public static Exception KeyAlreadyExists()
        => new InvalidOperationException("Specified key already exists.");
    public static Exception AlreadyInvoked(string methodName)
        => new InvalidOperationException($"'{methodName}' can be invoked just once.");
    public static Exception AlreadyInitialized(string? propertyName = null)
        => new InvalidOperationException(propertyName == null
            ? "Already initialized."
            : $"Property {propertyName} is already initialized.");

    public static Exception AlreadyLocked()
        => new InvalidOperationException($"The lock is already acquired by one of callers of the current method.");
    public static Exception AlreadyUsed()
        => new InvalidOperationException("The object was already used somewhere else.");
    public static Exception AlreadyCompleted()
        => new InvalidOperationException("The event source is already completed.");
    public static Exception ThisValueCanBeSetJustOnce()
        => new InvalidOperationException($"This value can be set just once.");
    public static Exception NoDefaultConstructor(Type type)
        => new InvalidOperationException($"Type '{type}' doesn't have a default constructor.");
    public static Exception NotInitialized(string? propertyName = null)
        => new InvalidOperationException(propertyName == null
            ? "Not initialized."
            : $"Property {propertyName} is not initialized.");

    public static Exception InternalError(string message)
        => new SystemException(message);

    public static Exception GenericMatchForConcreteType(Type type, Type matchType)
        => new InvalidOperationException($"Generic type '{matchType}' can't be a match for concrete type '{type}'.");
    public static Exception ConcreteMatchForGenericType(Type type, Type matchType)
        => new InvalidOperationException($"Concrete type '{matchType}' can't be a match for generic type '{type}'.");

    public static Exception TenantNotFound(Symbol tenantId)
        => new KeyNotFoundException($"Tenant '{tenantId.Value}' doesn't exist.");
}
