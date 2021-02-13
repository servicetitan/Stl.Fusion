
using System;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.CommandR;

namespace Stl.Fusion.Operations.Internal
{
    public readonly struct NestedCommand : IEquatable<NestedCommand>
    {
        public ICommand Command { get; }
        public OptionSet Items { get; }

        [JsonConstructor]
        public NestedCommand(ICommand command, OptionSet items)
        {
            Command = command;
            Items = items;
        }

        public override string ToString() => $"{GetType().Name}({Command})";

        // Equality

        public bool Equals(NestedCommand other) => Command.Equals(other.Command) && Items.Equals(other.Items);
        public override bool Equals(object? obj) => obj is NestedCommand other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Command, Items);
        public static bool operator ==(NestedCommand left, NestedCommand right) => left.Equals(right);
        public static bool operator !=(NestedCommand left, NestedCommand right) => !left.Equals(right);
    }
}
