using System.Net.WebSockets;
using System.Security;

namespace Stl.Fusion.Internal;

public static class Errors
{
    public static Exception TypeMustBeOpenGenericType(Type type)
        => new InvalidOperationException($"'{type}' must be open generic type.");
    public static Exception MustHaveASingleGenericArgument(Type type)
        => new InvalidOperationException($"'{type}' must have a single generic argument.");

    public static Exception WrongComputedState(
        ConsistencyState expectedState, ConsistencyState state)
        => new InvalidOperationException(
            $"Wrong Computed.State: expected {expectedState}, was {state}.");
    public static Exception WrongComputedState(ConsistencyState state)
        => new InvalidOperationException(
            $"Wrong Computed.State: {state}.");

    public static Exception ComputedCurrentIsNull()
        => new NullReferenceException($"Computed.Current() == null.");
    public static Exception ComputedCurrentIsOfIncompatibleType(Type expectedType)
        => new InvalidCastException(
            $"Computed.Current() can't be converted to '{expectedType}'.");
    public static Exception CapturedComputedIsOfIncompatibleType(Type expectedType)
        => new InvalidCastException(
            $"Computed.Captured() can't be converted to '{expectedType}'.");
    public static Exception NoComputedCaptured()
        => new InvalidOperationException($"No {nameof(IComputed)} was captured.");

    public static Exception ComputedInputCategoryCannotBeSet()
        => new NotSupportedException(
            "Only IState and IAnonymousComputedInput allow to manually set Category property.");

    public static Exception AnonymousComputedSourceIsNotComputedYet()
        => new InvalidOperationException("This anonymous computed source isn't computed yet.");

    public static Exception WebSocketConnectTimeout()
        => new WebSocketException("Connection timeout.");

    public static Exception ComputeServiceMethodAttributeOnStaticMethod(MethodInfo method)
        => new InvalidOperationException($"{nameof(ComputeMethodAttribute)} is applied to static method '{method}'.");
    public static Exception ComputeServiceMethodAttributeOnNonVirtualMethod(MethodInfo method)
        => new InvalidOperationException($"{nameof(ComputeMethodAttribute)} is applied to non-virtual method '{method}'.");
    public static Exception ComputeServiceMethodAttributeOnNonAsyncMethod(MethodInfo method)
        => new InvalidOperationException($"{nameof(ComputeMethodAttribute)} is applied to non-async method '{method}'.");
    public static Exception ComputeServiceMethodAttributeOnAsyncMethodReturningNonGenericTask(MethodInfo method)
        => new InvalidOperationException($"{nameof(ComputeMethodAttribute)} is applied to a method " +
            $"returning non-generic Task/ValueTask: '{method}'.");

    public static Exception InvalidContextCallOptions(CallOptions callOptions)
        => new InvalidOperationException(
            $"{nameof(ComputeContext)} with {nameof(CallOptions)} = {callOptions} cannot be used here.");

    // Session-related

    public static Exception InvalidSessionId(string parameterName)
        => new ArgumentOutOfRangeException(parameterName, "Provided Session.Id is invalid.");
    public static Exception SessionResolverSessionCannotBeSetForRootInstance()
        => new InvalidOperationException("ISessionResolver.Session can't be set for root (non-scoped) ISessionResolver.");

    public static Exception SessionUnavailable()
        => new SecurityException("The Session is unavailable.");
    public static Exception NotAuthenticated()
        => new SecurityException("You must sign in to perform this action.");
}
