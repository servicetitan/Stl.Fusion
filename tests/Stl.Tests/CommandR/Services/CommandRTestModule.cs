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
            Services.AddSingleton<LogCommandHandler>();
            Services.AddSingleton<DivCommandHandler>();
            Services.AddSingleton<RecSumCommandHandler>();
            Services.AddSingleton<LogEnterExitHandler>();

            Services.AddCommandR(b => {
                b.AddHandler<LogCommand, LogCommandHandler>();
                b.AddHandler<DivCommand, DivCommandHandler>();
                b.AddHandler<RecSumCommand, RecSumCommandHandler>();
                b.AddHandler<ICommand, LogEnterExitHandler>(-1000);
            });
        }
    }
}
