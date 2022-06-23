namespace Stl.Fusion.Multitenancy;

[DataContract]
public record TenantInfo(
    [property: DataMember] Symbol Id
    ) : IHasId<Symbol>
{
    public TenantInfo() : this(Symbol.Empty) { }
}
