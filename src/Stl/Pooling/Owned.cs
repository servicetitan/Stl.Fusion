namespace Stl.Pooling;

[StructLayout(LayoutKind.Auto)]
public readonly struct Owned<TItem, TOwner>(
    TItem subject,
    TOwner owner
    ) : IDisposable, IEquatable<Owned<TItem, TOwner>>
    where TOwner : IDisposable
{
    public TItem Subject { get; } = subject;
    public TOwner Owner { get; } = owner;

    public void Deconstruct(out TItem item, out TOwner owner)
    {
        item = Subject;
        owner = Owner;
    }

    public override string ToString() => $"{GetType().GetName()}({Subject} @ {Owner})";

    public void Dispose() => Owner?.Dispose();

    // Equality

    public bool Equals(Owned<TItem, TOwner> other)
        => EqualityComparer<TItem>.Default.Equals(Subject, other.Subject)
            && EqualityComparer<TOwner>.Default.Equals(Owner, other.Owner);
    public override bool Equals(object? obj) => obj is Owned<TItem, TOwner> other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Subject, Owner);
    public static bool operator ==(Owned<TItem, TOwner> left, Owned<TItem, TOwner> right) => left.Equals(right);
    public static bool operator !=(Owned<TItem, TOwner> left, Owned<TItem, TOwner> right) => !left.Equals(right);
}
