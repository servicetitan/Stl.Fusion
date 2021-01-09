using System;
using System.Reflection;
using Stl.CommandR.Configuration;

namespace Stl.CommandR.Internal
{
    public static class Errors
    {
        public static Exception CommandHandlerRegistryMustBeRegisteredAsInstance()
            => new InvalidOperationException("ICommandHandlerRegistry should be registered as instance.");
        public static Exception CommandHandlerRegistryInstanceIsNotRegistered()
            => new InvalidOperationException("ICommandHandlerRegistry instance is not registered.");
        public static Exception CommandResultTypeMismatch(Type expectedType, Type actualType)
            => new ArgumentException($"Command result type mismatch: expected '{expectedType}', got '{actualType}'");

        public static Exception NoCurrentCommandContext()
            => new InvalidOperationException("CommandContext.Current is null - no command is running.");

        public static Exception NoHandlerFound(ICommand command)
            => new InvalidOperationException($"No handler is found for command {command}.");
        public static Exception NoFinalHandlerFound(ICommand command)
            => new InvalidOperationException($"No final handler is found for command {command}.");

        public static Exception InvalidCommandHandlerMethod(MethodInfo methodInfo)
            => new InvalidOperationException($"Invalid command handler method: {methodInfo}.");
        public static Exception CommandHandlerMethodMustBeInstanceMethod(MethodInfo method)
            => new InvalidOperationException($"Command handler method must be instance method (non-static): '{method}'.");
        public static Exception CommandHandlerMethodMustReturnTask(MethodInfo methodInfo)
            => new InvalidOperationException($"Command handler method must return Task or Task<T>: {methodInfo}.");
        public static Exception WrongCommandHandlerMethodArgumentCount(MethodInfo methodInfo)
            => new InvalidOperationException($"Command handler method must have at least 2 arguments: command and CancellationToken.");
        public static Exception WrongCommandHandlerMethodArguments(MethodInfo methodInfo)
            => new InvalidOperationException($"Wrong command handler method arguments: {methodInfo}.");
        public static Exception WrongInterceptedCommandHandlerMethodSignature(MethodInfo methodInfo)
            => new InvalidOperationException(
                "Intercepted command handler method must be " +
                "public, virtual, and have exactly 2 arguments: " +
                "command and CancellationToken.");
    }
}
