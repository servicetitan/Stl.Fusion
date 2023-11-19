using System.Diagnostics.CodeAnalysis;
using Stl.CommandR.Interception;
using Stl.Interception;

namespace Stl.CommandR.Internal;

public static class CommanderProxies
{
    public static object NewServiceProxy(
        IServiceProvider services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type implementationType)
    {
        // We should try to validate it here because if the type doesn't
        // have any virtual methods (which might be a mistake), no calls
        // will be intercepted, so no error will be thrown later.

        var interceptor = services.GetRequiredService<CommandServiceInterceptor>();
        interceptor.ValidateType(implementationType);
        var proxy = services.ActivateProxy(implementationType, interceptor);
        return proxy;
    }
}
