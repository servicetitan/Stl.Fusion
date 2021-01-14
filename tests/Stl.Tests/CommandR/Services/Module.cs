using Stl.DependencyInjection;
using Stl.Extensibility;

namespace Stl.Tests.CommandR.Services
{
    [Module]
    public class Module : ModuleBase
    {
        public override void ConfigureServices()
        {
            Services.AttributeScanner()
                .WithTypeFilter(GetType().Namespace!)
                .AddServicesFrom(GetType().Assembly);
        }
    }
}
