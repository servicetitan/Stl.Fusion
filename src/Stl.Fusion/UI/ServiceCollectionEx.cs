using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.UI
{
    public static class ServiceCollectionEx
    {
        // AddLiveUpdater

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
    }
}
