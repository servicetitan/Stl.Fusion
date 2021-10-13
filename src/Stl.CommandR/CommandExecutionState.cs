using System;
using System.Collections.Generic;
using Stl.CommandR.Configuration;

namespace Stl.CommandR
{
    public readonly struct CommandExecutionState : IEquatable<CommandExecutionState>
    {
        public IReadOnlyList<CommandHandler> Handlers { get; }
        public int NextHandlerIndex { get; }
        public bool IsFinal => NextHandlerIndex >= Handlers.Count;
        public CommandHandler NextHandler => Handlers[NextHandlerIndex];
        public CommandExecutionState NextExecutionState => new(Handlers, NextHandlerIndex + 1);

        public CommandExecutionState(IReadOnlyList<CommandHandler> handlers, int nextHandlerIndex = 0)
        {
            Handlers = handlers;
            NextHandlerIndex = nextHandlerIndex;
        }

        public void Deconstruct(out IReadOnlyList<CommandHandler> handlers, out int nextHandlerIndex)
        {
            handlers = Handlers;
            nextHandlerIndex = NextHandlerIndex;
        }

        public override string ToString()
            => $"{GetType().Name}({NextHandlerIndex}/{Handlers.Count})";

        public CommandHandler? FindFinalHandler()
            => FindFinalHandler(NextHandlerIndex);
        public CommandHandler? FindFinalHandler(int startIndex)
        {
            for (var i = startIndex; i < Handlers.Count; i++) {
                var handler = Handlers[i];
                if (!handler.IsFilter)
                    return handler;
            }
            return null;
        }

        // Equality

        public bool Equals(CommandExecutionState other)
            => Handlers.Equals(other.Handlers) && NextHandlerIndex == other.NextHandlerIndex;
        public override bool Equals(object? obj)
            => obj is CommandExecutionState other && Equals(other);
        public override int GetHashCode()
            => HashCode.Combine(Handlers, NextHandlerIndex);
        public static bool operator ==(CommandExecutionState left, CommandExecutionState right)
            => left.Equals(right);
        public static bool operator !=(CommandExecutionState left, CommandExecutionState right)
            => !left.Equals(right);
    }
}
