using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Extensibility;

namespace Stl.Tests.CommandR.Services
{
    [RegisterModule]
    public class Module : ModuleBase
    {
        public Module(IServiceCollection services) : base(services) { }

        public override void Use()
        {
            Services.UseRegisterAttributeScanner()
                .WithTypeFilter(GetType().Namespace!)
                .RegisterFrom(GetType().Assembly);
        }
    }
}
