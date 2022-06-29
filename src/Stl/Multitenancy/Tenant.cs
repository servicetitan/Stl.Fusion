namespace Stl.Multitenancy;

[DataContract]
public record Tenant : IHasId<Symbol>
{
    public static Tenant Default { get; } = new(Symbol.Empty, "The only tenant", "");
    public static Tenant Example { get; } = new("*", "Example tenant", "__example");

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

    // Conversion

    public static implicit operator Symbol(Tenant tenant) => tenant.Id;

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
