using System.Collections.Immutable;
using Stl.CommandR.Internal;

namespace Stl.CommandR.Configuration
{
    public interface ICommandHandlerRegistry
    {
        ImmutableHashSet<CommandHandler> Handlers { get; set; }
        bool TryAdd(CommandHandler handler);
        void Add(CommandHandler handler);
        void Clear();
    }

    public class CommandHandlerRegistry : ICommandHandlerRegistry
    {
        public ImmutableHashSet<CommandHandler> Handlers { get; set; } =
            ImmutableHashSet<CommandHandler>.Empty;

        public bool TryAdd(CommandHandler handler)
        {
            var oldHandlers = Handlers;
            var newHandlers = Handlers = Handlers.Add(handler);
            return oldHandlers != newHandlers;
        }

        public void Add(CommandHandler handler)
        {
            if (!TryAdd(handler))
                throw Errors.HandlerIsAlreadyAdded(handler);
        }

        public void Clear()
            => Handlers = ImmutableHashSet<CommandHandler>.Empty;
    }
}
