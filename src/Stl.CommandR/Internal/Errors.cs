using System;
using System.Reflection;
using Stl.CommandR.Configuration;

namespace Stl.CommandR.Internal
{
    public static class Errors
    {
        public static Exception CommandResultTypeMismatch(Type expectedType, Type actualType)
            => new ArgumentException($"Command result type mismatch: expected '{expectedType}', got '{actualType}'");

        public static Exception CommandHandlerRegistryMustBeRegisteredAsInstance()
            => new InvalidOperationException("ICommandHandlerRegistry should be registered as instance.");

        public static Exception CommandHandlerRegistryInstanceIsNotRegistered()
            => new InvalidOperationException("ICommandHandlerRegistry instance is not registered.");

        public static Exception HandlerIsAlreadyAdded(CommandHandler handler)
            => new InvalidOperationException($"Command handler {handler} is already added.");

        public static Exception NoCurrentCommandContext()
            => new InvalidOperationException("CommandContext.Current is null - no command is running.");

        public static Exception NoHandlerFound(ICommand command)
            => new InvalidOperationException($"No handler is found for command {command}.");

        public static Exception NoFinalHandlerFound(ICommand command)
            => new InvalidOperationException($"No final handler is found for command {command}.");

        public static Exception InvalidCommandHandlerMethod(MethodInfo handlerMethod)
            => new InvalidOperationException($"Invalid command handler method: {handlerMethod}.");
    }
}
