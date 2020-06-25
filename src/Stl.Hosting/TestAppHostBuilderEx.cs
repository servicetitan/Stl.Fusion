using System;

namespace Stl.Hosting
{
    public static class TestAppHostBuilderEx
    {
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
