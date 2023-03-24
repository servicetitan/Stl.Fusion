using Stl.RegisterAttributes;

namespace Stl.Fusion.Client;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class RegisterRestEaseClientAttribute : RegisterAttribute
{
    public override void Register(IServiceCollection services, Type implementationType)
        => services.AddFusion().AddRestEaseClient().AddClientService(implementationType);
}
