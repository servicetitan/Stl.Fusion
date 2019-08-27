using System;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace Stl.Plugins.Extensions.Hosting
{
    public static class AppHostBuilderEx
    {
        public static TBuilder ConfigurePlugins<TBuilder>(this TBuilder builder, 
            Func<IPluginHostBuilder, IPluginHostBuilder> configurator)
            where TBuilder : IAppHostBuilder
        {
            builder.PluginHostBuilder = configurator.Invoke(builder.PluginHostBuilder);
            return builder;
        }

        public static TBuilder ConfigureWebHost<TBuilder>(this TBuilder builder, 
            Func<IWebHostBuilder, IWebHostBuilder> configurator)
            where TBuilder : IAppHostBuilder
        {
            builder.WebHostBuilder = configurator.Invoke(builder.WebHostBuilder);
            return builder;
        }

        public static THost Build<THost>(this IAppHostBuilder<THost> builder)
            where THost : class, IAppHost
        {
            var host = builder.Implementation.CreateHost();
            var implementation = host.Implementation;
            implementation.BuildFrom(builder);
            return (THost) host;
        }

        public static THost BuildAndStart<THost>(this IAppHostBuilder<THost> builder,
            string arguments, CancellationToken cancellationToken = default)
            where THost : class, IAppHost
        {
            var host = builder.Build();
            host.Implementation.Start(arguments, cancellationToken);
            return host;
        }
    }
}
