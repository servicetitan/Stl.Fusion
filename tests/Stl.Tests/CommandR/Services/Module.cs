using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Extensibility;

namespace Stl.Tests.CommandR.Services
{
    [Module]
    public class Module : ModuleBase
    {
        public Module(IServiceCollection services) : base(services) { }

        public override void Use()
        {
            Services.AttributeScanner()
                .WithTypeFilter(GetType().Namespace!)
                .AddServicesFrom(GetType().Assembly);
        }
    }
}
