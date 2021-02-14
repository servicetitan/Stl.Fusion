
using System;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.CommandR;

namespace Stl.Fusion.Operations.Internal
{
    public readonly struct NestedCommandEntry : IEquatable<NestedCommandEntry>
    {
        public ICommand Command { get; }
        public OptionSet Items { get; }

        [JsonConstructor]
        public NestedCommandEntry(ICommand command, OptionSet items)
        {
            Command = command;
            Items = items;
        }

        public void Deconstruct(out ICommand command, out OptionSet items)
        {
            command = Command;
            items = Items;
        }

        public override string ToString() => $"{GetType().Name}({Command})";

        // Equality

        public bool Equals(NestedCommandEntry other) => Command.Equals(other.Command) && Items.Equals(other.Items);
        public override bool Equals(object? obj) => obj is NestedCommandEntry other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Command, Items);
        public static bool operator ==(NestedCommandEntry left, NestedCommandEntry right) => left.Equals(right);
        public static bool operator !=(NestedCommandEntry left, NestedCommandEntry right) => !left.Equals(right);
    }
}
