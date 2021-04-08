using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pluralize.NET;
using Stl.Fusion.Extensions.Internal;

namespace Stl.Fusion.Extensions
{
    public static class FusionBuilderEx
    {
        public static FusionBuilder AddLiveClock(this FusionBuilder fusion,
            Action<IServiceProvider, LiveClock.Options>? optionsBuilder = null)
        {
            var services = fusion.Services;
            services.TryAddSingleton<IPluralize>(new Pluralizer());
            services.TryAddSingleton(c => {
                var options = new LiveClock.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            fusion.AddComputeService<ILiveClock, LiveClock>();
            return fusion;
        }

        public static FusionBuilder AddIsolatedKeyValueStore(this FusionBuilder fusion,
            Action<IServiceProvider, IsolatedKeyValueStore.Options>? optionsBuilder = null)
        {
            var services = fusion.Services;
            services.TryAddSingleton(c => {
                var options = new IsolatedKeyValueStore.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            fusion.AddComputeService<IIsolatedKeyValueStore, IsolatedKeyValueStore>();
            return fusion;
        }
    }
}
