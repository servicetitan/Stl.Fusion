namespace Stl.Fusion.EntityFramework.Multitenancy;

[DataContract]
public record TenantInfo(
    [property: DataMember] string Id
    ) : IHasId<string>
{
    public TenantInfo() : this("") { }
}
