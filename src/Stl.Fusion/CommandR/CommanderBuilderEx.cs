using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.Fusion.CommandR.Internal;

namespace Stl.Fusion.CommandR
{
    public static class CommanderBuilderEx
    {
        public static CommanderBuilder AddInvalidatingHandler(this CommanderBuilder commander, double? priorityOverride = null)
        {
            commander.Services.AddSingleton<InvalidatingHandler>();
            return commander.AddHandlers<InvalidatingHandler>(priorityOverride);
        }
    }
}
