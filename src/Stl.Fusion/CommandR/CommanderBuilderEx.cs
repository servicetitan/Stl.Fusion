using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.Fusion.CommandR.Internal;

namespace Stl.Fusion.CommandR
{
    public static class CommanderBuilderEx
    {
        public static CommanderBuilder AddInvalidatingCommandHandler(this CommanderBuilder commander, double? priorityOverride = null)
        {
            commander.Services.AddSingleton<InvalidatingCommandHandler>();
            return commander.AddHandlers<InvalidatingCommandHandler>(priorityOverride);
        }
    }
}
