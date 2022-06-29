using Stl.RegisterAttributes;

namespace Stl.Fusion.Client;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class RegisterRestEaseReplicaServiceAttribute : RegisterAttribute
{
    public Type? ServiceType { get; set; }

    public RegisterRestEaseReplicaServiceAttribute(Type? serviceType = null)
        => ServiceType = serviceType;

    public override void Register(IServiceCollection services, Type implementationType)
        => services
            .AddFusion()
            .AddRestEaseClient()
            .AddReplicaService(ServiceType ?? implementationType, implementationType);
}
