namespace Stl.DependencyInjection.Internal;

[DataContract]
public record ServiceTypeRef(
    [property: DataMember(Order = 0)] TypeRef TypeRef
    ) : ServiceRef
{
    public override object? TryResolve(IServiceProvider services)
        => services.GetService(TypeRef.Resolve());

    public ServiceTypeRef() : this(TypeRef.None) { }
}
