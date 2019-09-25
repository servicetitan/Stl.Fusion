using System;

namespace Stl.Hosting
{
    public static class AppHostBuilderEx
    {
        public static TBuilder Configure<TBuilder>(this TBuilder builder, 
            Action<TBuilder> configurator)
            where TBuilder : class, IAppHostBuilder
        {
            configurator.Invoke(builder);
            return builder;
        }

        public static TBuilder ConfigureTestHost<TBuilder>(this TBuilder builder,
            Type[] testPluginTypes,
            Action<ITestAppHostBuilder> configurator)
            where TBuilder : class, ITestAppHostBuilder
        {
            builder.TestPluginTypes = testPluginTypes;
            configurator.Invoke(builder);
            return builder;
        }
    }
}
