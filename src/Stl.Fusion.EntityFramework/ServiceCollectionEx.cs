using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR;
using Stl.DependencyInjection;
using Stl.Fusion.EntityFramework.Reprocessing;

namespace Stl.Fusion.EntityFramework
{
    public static class ServiceCollectionEx
    {
        public static DbContextBuilder<TDbContext> AddDbContextServices<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
            => new(services);

        public static IServiceCollection AddDbContextServices<TDbContext>(
            this IServiceCollection services,
            Action<DbContextBuilder<TDbContext>> configureDbContext)
            where TDbContext : DbContext
        {
            var dbContextServices = services.AddDbContextServices<TDbContext>();
            configureDbContext.Invoke(dbContextServices);
            return services;
        }

        public static IServiceCollection AddCommandReprocessor(
            this IServiceCollection services,
            Action<IServiceProvider, CommandReprocessor.Options>? optionsBuilder = null)
            => services.AddCommandReprocessor<CommandReprocessor>(optionsBuilder);

        public static IServiceCollection AddCommandReprocessor<TCommandReprocessor>(
            this IServiceCollection services,
            Action<IServiceProvider, CommandReprocessor.Options>? optionsBuilder = null)
            where TCommandReprocessor : CommandReprocessor
        {
            services.TryAddSingleton(c => {
                var options = new CommandReprocessor.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            if (!services.HasService<CommandReprocessor>()) {
                services.AddSingleton<CommandReprocessor, TCommandReprocessor>();
                services.AddCommander().AddHandlers<CommandReprocessor>();
            }
            return services;
        }
    }
}
