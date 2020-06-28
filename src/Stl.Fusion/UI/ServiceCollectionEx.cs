using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.UI
{
    public static class ServiceCollectionEx
    {
        // AddAllLive

        public static IServiceCollection AddAllLive(
            this IServiceCollection services,
            Assembly fromAssembly,
            Action<IServiceProvider, ILiveOptions>? optionsBuilder = null)
        {
            var tLiveUpdater = typeof(ILiveUpdater);
            var tLiveUpdaterGeneric = typeof(ILiveUpdater<>);
            foreach (var tUpdater in fromAssembly.DefinedTypes) {
                if (tUpdater.IsNotPublic || tUpdater.IsAbstract || tUpdater.IsValueType)
                    continue;
                if (!tLiveUpdater.IsAssignableFrom(tUpdater))
                    continue;
                foreach (var tImplemented in tUpdater.ImplementedInterfaces) {
                    if (!tImplemented.IsConstructedGenericType)
                        continue;
                    if (!tLiveUpdater.IsAssignableFrom(tImplemented))
                        continue;
                    var tBase = tImplemented.GetGenericTypeDefinition();
                    if (tBase != tLiveUpdaterGeneric)
                        continue;
                    var tModel = tImplemented.GetGenericArguments().SingleOrDefault();
                    if (tModel == null)
                        continue;
                    AddLive(services, tModel, tUpdater, optionsBuilder);
                }
            }
            return services;
        }

        // AddLiveUpdater

        public static IServiceCollection AddLiveUpdater(
            this IServiceCollection services, Type modelType, Type updaterType)
        {
            var iUpdaterType = typeof(ILiveUpdater<>).MakeGenericType(modelType);
            if (typeof(IComputedService).IsAssignableFrom(updaterType))
                services.AddComputedService(iUpdaterType, updaterType);
            else 
                services.TryAddSingleton(iUpdaterType, updaterType);
            return services;
        }

        public static IServiceCollection AddLiveUpdater<TModel, TLiveUpdater>(
            this IServiceCollection services)
            where TLiveUpdater : class, ILiveUpdater<TModel>
            where TModel : class, new()
        {
            if (typeof(IComputedService).IsAssignableFrom(typeof(TLiveUpdater)))
                services.AddComputedService<ILiveUpdater<TModel>, TLiveUpdater>();
            else 
                services.TryAddSingleton<ILiveUpdater<TModel>, TLiveUpdater>();
            return services;
        }

        // AddLive

        public static IServiceCollection AddLive(
            this IServiceCollection services,
            Type modelType, Type updaterType,
            Action<IServiceProvider, ILiveOptions>? optionsBuilder = null)
        {
            var mAddLive = typeof(ServiceCollectionEx).GetMethod(
                nameof(AddLiveInternal),
                BindingFlags.Static | BindingFlags.NonPublic);
            var result = mAddLive
                .MakeGenericMethod(modelType, updaterType)
                .Invoke(null, new object?[] { services, optionsBuilder });
            return (IServiceCollection) result;
        }

        public static IServiceCollection AddLive<TModel, TUpdater>(
            this IServiceCollection services,
            Action<IServiceProvider, Live<TModel>.Options>? optionsBuilder = null)
            where TModel : class, new()
            where TUpdater : class, ILiveUpdater<TModel>
        {
            services.TryAddSingleton(c => {
                var updater = c.GetRequiredService<ILiveUpdater<TModel>>();
                var options = new Live<TModel>.Options() {
                    Default = new TModel(),
                    Updater = (prev, cancellationToken) => updater.UpdateAsync(prev, cancellationToken),
                };
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddTransient<ILive<TModel>, Live<TModel>>();
            services.AddLiveUpdater<TModel, TUpdater>();
            return services;
        }

        public static IServiceCollection AddLive<T>(
            this IServiceCollection services,
            Func<IServiceProvider, IComputed<T>, CancellationToken, Task<T>> updater,
            Action<IServiceProvider, Live<T>.Options>? optionsBuilder = null)
        {
            services.TryAddSingleton(c => {
                var options = new Live<T>.Options() {
                    Updater = (prev, cancellationToken) => updater.Invoke(c, prev, cancellationToken),
                };
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddTransient<ILive<T>, Live<T>>();
            return services;
        }

        // Private methods

        // This method is here just to simplify calling
        // the downstream method (AddLive) via reflection.
        private static IServiceCollection AddLiveInternal<TModel, TUpdater>(
            this IServiceCollection services,
            Action<IServiceProvider, Live<TModel>.Options>? optionsBuilder = null)
            where TModel : class, new()
            where TUpdater : class, ILiveUpdater<TModel>
            => services.AddLive<TModel, TUpdater>(optionsBuilder);
    }
}
