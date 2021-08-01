using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.UI
{
    public static class ServiceProviderEx
    {
        public static IUICommandTracker UICommandTracker(this IServiceProvider services)
            => services.GetService<IUICommandTracker>() ?? UI.UICommandTracker.None;

        public static UICommandRunner UICommandRunner(this IServiceProvider services)
            => services.GetRequiredService<UICommandRunner>();
    }
}
