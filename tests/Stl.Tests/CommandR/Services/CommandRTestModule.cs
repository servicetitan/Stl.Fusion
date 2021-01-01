using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.DependencyInjection;

namespace Stl.Tests.CommandR.Services
{
    [Module(Scope = nameof(CommandRTestModule))]
    public class CommandRTestModule : ModuleBase
    {
        public override void ConfigureServices()
        {
            Services.AddSingleton<LogEnterExitService>();
            Services.AddSingleton<LogCommandHandler>();
            Services.AddSingleton<MathService>();

            Services.AddCommandR(b => {
                b.AddHandler<LogCommand, LogCommandHandler>();
                b.AddHandlers<LogEnterExitService>();
                b.AddHandlers<MathService>();
            });
        }
    }
}
