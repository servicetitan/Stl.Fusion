using Stl.RegisterAttributes;

namespace Stl.Fusion.Client;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class RegisterRestEaseClientAttribute : RegisterAttribute
{
    public Type? ServiceType { get; set; }

    public RegisterRestEaseClientAttribute(Type? serviceType = null)
        => ServiceType = serviceType;

    public override void Register(IServiceCollection services, Type implementationType)
        => services.AddFusion().AddRestEaseClient().AddClientService(ServiceType ?? implementationType, implementationType);
}
