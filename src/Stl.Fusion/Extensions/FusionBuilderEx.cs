using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pluralize.NET;
using Stl.Fusion.Extensions.Internal;

namespace Stl.Fusion.Extensions
{
    public static class FusionBuilderEx
    {
        public static FusionBuilder AddFusionTime(this FusionBuilder fusion,
            Action<IServiceProvider, FusionTime.Options>? optionsBuilder = null)
        {
            var services = fusion.Services;
            services.TryAddSingleton<IPluralize>(new Pluralizer());
            services.TryAddSingleton(c => {
                var options = new FusionTime.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            fusion.AddComputeService<IFusionTime, Internal.FusionTime>();
            return fusion;
        }

        public static FusionBuilder AddInMemoryKeyValueStore(this FusionBuilder fusion,
            Action<IServiceProvider, InMemoryKeyValueStore.Options>? optionsBuilder = null)
        {
            var services = fusion.Services;
            services.TryAddSingleton(c => {
                var options = new InMemoryKeyValueStore.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            fusion.AddComputeService<IKeyValueStore, InMemoryKeyValueStore>();
            services.AddHostedService(c => (InMemoryKeyValueStore) c.GetRequiredService<IKeyValueStore>());
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
