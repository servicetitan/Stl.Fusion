using System;

namespace Stl.Hosting
{
    public static class AppHostBuilderEx
    {
        public static TBuilder Configure<TBuilder>(this TBuilder builder, Action<TBuilder> configurator)
            where TBuilder : class, IAppHostBuilder
        {
            configurator.Invoke(builder);
            return builder;
        }

        public static TBuilder ConfigureForTest<TBuilder>(this TBuilder builder, Action<ITestAppHostBuilder> configurator)
            where TBuilder : class, ITestAppHostBuilder
        {
            configurator.Invoke(builder);
            return builder;
        }
    }
}
