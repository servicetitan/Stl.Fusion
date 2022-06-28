namespace Stl.Multitenancy;

[DataContract]
public record Tenant : IHasId<Symbol>
{
    public static Tenant Single { get; } = new(Symbol.Empty, "The only tenant", "");

    [DataMember] public Symbol Id { get; init; } = Symbol.Empty;
    [DataMember] public string Title { get; init; } = "";
    [DataMember] public string StorageId { get; init; } = "";

    public Tenant() { }
    public Tenant(Symbol id, string? title = null, string? storageId = null)
    {
        Id = id;
        Title = title ?? id.Value;
        StorageId = storageId ?? id.Value;
    }

    // Equality

    public virtual bool Equals(Tenant? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
