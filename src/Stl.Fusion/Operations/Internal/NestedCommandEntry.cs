namespace Stl.Fusion.Operations.Internal;

[method: JsonConstructor, Newtonsoft.Json.JsonConstructor]
public readonly struct NestedCommandEntry(ICommand command, OptionSet items)
    : IEquatable<NestedCommandEntry>
{
    public ICommand Command { get; } = command;
    public OptionSet Items { get; } = items;

    public void Deconstruct(out ICommand command, out OptionSet items)
    {
        command = Command;
        items = Items;
    }

    public override string ToString() => $"{GetType().GetName()}({Command})";

    // Equality

    public bool Equals(NestedCommandEntry other) => Command.Equals(other.Command) && Items.Equals(other.Items);
    public override bool Equals(object? obj) => obj is NestedCommandEntry other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Command, Items);
    public static bool operator ==(NestedCommandEntry left, NestedCommandEntry right) => left.Equals(right);
    public static bool operator !=(NestedCommandEntry left, NestedCommandEntry right) => !left.Equals(right);
}
