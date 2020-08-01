using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Fusion.UI
{
    public static class ServiceCollectionEx
    {
        // AddLiveUpdater

        public static IServiceCollection AddLiveStateUpdater(
            this IServiceCollection services,
            Type stateType, Type updaterType)
        {
            var iUpdaterType = typeof(ILiveStateUpdater<>).MakeGenericType(stateType);
            if (typeof(IComputedService).IsAssignableFrom(updaterType))
                services.AddComputedService(iUpdaterType, updaterType);
            else
                services.TryAddSingleton(iUpdaterType, updaterType);
            return services;
        }

        public static IServiceCollection AddLiveStateUpdater(
            this IServiceCollection services,
            Type localType, Type stateType, Type updaterType)
        {
            var iUpdaterType = typeof(ILiveStateUpdater<,>).MakeGenericType(localType, stateType);
            if (typeof(IComputedService).IsAssignableFrom(updaterType))
                services.AddComputedService(iUpdaterType, updaterType);
            else
                services.TryAddSingleton(iUpdaterType, updaterType);
            return services;
        }

        public static IServiceCollection AddLiveStateUpdater<TState, TLiveUpdater>(
            this IServiceCollection services)
            where TLiveUpdater : class, ILiveStateUpdater<TState>
            where TState : class
        {
            if (typeof(IComputedService).IsAssignableFrom(typeof(TLiveUpdater)))
                services.AddComputedService<ILiveStateUpdater<TState>, TLiveUpdater>();
            else
                services.TryAddSingleton<ILiveStateUpdater<TState>, TLiveUpdater>();
            return services;
        }

        public static IServiceCollection AddLiveStateUpdater<TLocal, TState, TLiveUpdater>(
            this IServiceCollection services)
            where TLiveUpdater : class, ILiveStateUpdater<TLocal, TState>
            where TState : class
        {
            if (typeof(IComputedService).IsAssignableFrom(typeof(TLiveUpdater)))
                services.AddComputedService<ILiveStateUpdater<TLocal, TState>, TLiveUpdater>();
            else
                services.TryAddSingleton<ILiveStateUpdater<TLocal, TState>, TLiveUpdater>();
            return services;
        }

        // AddLive

        public static IServiceCollection AddLiveState(
            this IServiceCollection services,
            Type stateType, Type updaterType,
            Action<IServiceProvider, LiveState.Options>? optionsBuilder = null)
        {
            var mAddLive = typeof(ServiceCollectionEx).GetMethod(
                nameof(AddLiveStateInternal1),
                BindingFlags.Static | BindingFlags.NonPublic);
            var result = mAddLive
                .MakeGenericMethod(stateType, updaterType)
                .Invoke(null, new object?[] { services, optionsBuilder });
            return (IServiceCollection) result;
        }

        public static IServiceCollection AddLiveState(
            this IServiceCollection services,
            Type localType, Type stateType, Type updaterType,
            Action<IServiceProvider, LiveState.Options>? optionsBuilder = null)
        {
            var mAddLive = typeof(ServiceCollectionEx).GetMethod(
                nameof(AddLiveStateInternal2),
                BindingFlags.Static | BindingFlags.NonPublic);
            var result = mAddLive
                .MakeGenericMethod(localType, stateType, updaterType)
                .Invoke(null, new object?[] { services, optionsBuilder });
            return (IServiceCollection) result;
        }

        public static IServiceCollection AddLiveState<TState, TUpdater>(
            this IServiceCollection services,
            Action<IServiceProvider, LiveState<TState>.Options>? optionsBuilder = null)
            where TState : class
            where TUpdater : class, ILiveStateUpdater<TState>
        {
            services.TryAddSingleton(c => {
                var updater = c.GetRequiredService<ILiveStateUpdater<TState>>();
                var options = new LiveState<TState>.Options() {
                    Updater = (live, cancellationToken) => updater.UpdateAsync(live, cancellationToken),
                };
                var untypedOptionsBuilder = optionsBuilder
                    ?? c.GetService<Action<IServiceProvider, LiveState<TState>.Options>>()
                    ?? c.GetService<Action<IServiceProvider, LiveState.Options>>();
                untypedOptionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddTransient<ILiveState<TState>, LiveState<TState>>();
            services.AddLiveStateUpdater<TState, TUpdater>();
            return services;
        }

        public static IServiceCollection AddLiveState<TLocal, TState, TUpdater>(
            this IServiceCollection services,
            Action<IServiceProvider, LiveState<TLocal, TState>.Options>? optionsBuilder = null)
            where TState : class
            where TUpdater : class, ILiveStateUpdater<TLocal, TState>
        {
            services.TryAddSingleton(c => {
                var updater = c.GetRequiredService<ILiveStateUpdater<TLocal, TState>>();
                var options = new LiveState<TLocal, TState>.Options() {
                    Updater = (live, cancellationToken) => updater.UpdateAsync(live, cancellationToken),
                };
                var untypedOptionsBuilder = optionsBuilder
                    ?? c.GetService<Action<IServiceProvider, LiveState<TLocal, TState>.Options>>()
                    ?? c.GetService<Action<IServiceProvider, LiveState.Options>>();
                untypedOptionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddTransient<ILiveState<TLocal, TState>, LiveState<TLocal, TState>>();
            services.TryAddTransient<ILiveState<TState>, LiveState<TLocal, TState>>();
            services.AddLiveStateUpdater<TLocal, TState, TUpdater>();
            return services;
        }

        public static IServiceCollection AddLiveState<TState>(
            this IServiceCollection services,
            Func<IServiceProvider, ILiveState<TState>, CancellationToken, Task<TState>> updater,
            Action<IServiceProvider, LiveState<TState>.Options>? optionsBuilder = null)
        {
            services.TryAddSingleton(c => {
                var options = new LiveState<TState>.Options() {
                    Updater = (live, cancellationToken) => updater.Invoke(c, live, cancellationToken),
                };
                var untypedOptionsBuilder = optionsBuilder
                    ?? c.GetService<Action<IServiceProvider, LiveState<TState>.Options>>()
                    ?? c.GetService<Action<IServiceProvider, LiveState.Options>>();
                untypedOptionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddTransient<ILiveState<TState>, LiveState<TState>>();
            return services;
        }

        public static IServiceCollection AddLiveState<TLocal, TState>(
            this IServiceCollection services,
            Func<IServiceProvider, ILiveState<TLocal, TState>, CancellationToken, Task<TState>> updater,
            Action<IServiceProvider, LiveState<TLocal, TState>.Options>? optionsBuilder = null)
        {
            services.TryAddSingleton(c => {
                var options = new LiveState<TLocal, TState>.Options() {
                    Updater = (live, cancellationToken) => updater.Invoke(c, live, cancellationToken),
                };
                var untypedOptionsBuilder = optionsBuilder
                    ?? c.GetService<Action<IServiceProvider, LiveState<TLocal, TState>.Options>>()
                    ?? c.GetService<Action<IServiceProvider, LiveState.Options>>();
                untypedOptionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddTransient<ILiveState<TLocal, TState>, LiveState<TLocal, TState>>();
            services.TryAddTransient<ILiveState<TState>, LiveState<TLocal, TState>>();
            return services;
        }

        // Private methods

        // This method is here just to simplify calling
        // the downstream method (AddLive) via reflection.
        private static IServiceCollection AddLiveStateInternal1<TState, TUpdater>(
            this IServiceCollection services,
            Action<IServiceProvider, LiveState<TState>.Options>? optionsBuilder = null)
            where TState : class
            where TUpdater : class, ILiveStateUpdater<TState>
            => services.AddLiveState<TState, TUpdater>(optionsBuilder);

        // This method is here just to simplify calling
        // the downstream method (AddLive) via reflection.
        private static IServiceCollection AddLiveStateInternal2<TLocal, TState, TUpdater>(
            this IServiceCollection services,
            Action<IServiceProvider, LiveState<TLocal, TState>.Options>? optionsBuilder = null)
            where TState : class
            where TUpdater : class, ILiveStateUpdater<TLocal, TState>
            => services.AddLiveState<TLocal, TState, TUpdater>(optionsBuilder);
    }
}
