using System;
using System.Collections.Immutable;

namespace Stl.CommandR.Internal
{
    public interface ICommandHandlerRegistry
    {
        ImmutableHashSet<CommandHandler> Handlers { get; }
        bool TryAddHandler(CommandHandler handler);
        void AddHandler(CommandHandler handler);
    }

    public class CommandHandlerRegistry : ICommandHandlerRegistry
    {
        public ImmutableHashSet<CommandHandler> Handlers { get; protected set; } =
            ImmutableHashSet<CommandHandler>.Empty;

        public bool TryAddHandler(CommandHandler handler)
        {
            var oldHandlers = Handlers;
            var newHandlers = Handlers = Handlers.Add(handler);
            return oldHandlers != newHandlers;
        }

        public void AddHandler(CommandHandler handler)
        {
            if (!TryAddHandler(handler))
                throw new InvalidOperationException($"{handler} is already added.");
        }
    }
}
