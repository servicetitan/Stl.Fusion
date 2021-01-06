using System.Collections.Immutable;

namespace Stl.CommandR.Configuration
{
    public interface ICommandHandlerRegistry
    {
        ImmutableHashSet<CommandHandler> Handlers { get; set; }
        bool Add(CommandHandler handler);
        void Clear();
    }

    public class CommandHandlerRegistry : ICommandHandlerRegistry
    {
        public ImmutableHashSet<CommandHandler> Handlers { get; set; } =
            ImmutableHashSet<CommandHandler>.Empty;

        public bool Add(CommandHandler handler)
        {
            var oldHandlers = Handlers;
            var newHandlers = Handlers = Handlers.Add(handler);
            return oldHandlers != newHandlers;
        }

        public void Clear()
            => Handlers = ImmutableHashSet<CommandHandler>.Empty;
    }
}
