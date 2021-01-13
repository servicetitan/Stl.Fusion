using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR;
using Stl.Fusion.CommandR.Internal;

namespace Stl.Fusion.CommandR
{
    public static class CommanderBuilderEx
    {
        public static CommanderBuilder AddInvalidationHandler(this CommanderBuilder commander, double? orderOverride = null)
        {
            var services = commander.Services;
            services.TryAddSingleton<IInvalidationInfoProvider, InvalidationInfoProvider>();
            services.TryAddSingleton<InvalidationHandler.Options>();
            services.TryAddSingleton<InvalidationHandler>();
            return commander.AddHandlers<InvalidationHandler>(orderOverride);
        }
    }
}
