using Stl.DependencyInjection;

namespace Stl.Tests.CommandR.Services
{
    [Module(Scope = nameof(CommandRTestModule))]
    public class CommandRTestModule : ModuleBase
    {
        public override void ConfigureServices()
        {
            Services.AttributeScanner()
                .WithTypeFilter(GetType().Namespace!)
                .AddServicesFrom(GetType().Assembly);
        }
    }
}
