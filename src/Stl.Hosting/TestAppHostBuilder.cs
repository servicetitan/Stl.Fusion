using System;

namespace Stl.Hosting
{
    public interface ITestAppHostBuilder
    {
        ReadOnlyMemory<Type> TestPluginTypes { get; set; }
        
        ITestAppHostBuilderImpl Implementation { get; }

        ITestAppHostBuilder InjectPreBuilder<TBuilder>(Action<TBuilder> preBuilder)
            where TBuilder : class;
        ITestAppHostBuilder InjectPostBuilder<TBuilder>(Action<TBuilder> postBuilder)
            where TBuilder : class;
    }

    public interface ITestAppHostBuilderImpl
    {
        void InvokePreBuilders<TBuilder>(TBuilder builder)
            where TBuilder : class;
        void InvokePostBuilders<TBuilder>(TBuilder builder)
            where TBuilder : class;
    }
}