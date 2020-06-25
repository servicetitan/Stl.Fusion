using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Plugins.Services 
{
    public class QueryingPluginFactory : PluginFactory
    {
        public IPluginInfoQuery Query { get; } 

        public QueryingPluginFactory(IServiceProvider services) : base(services) 
            => Query = services.GetRequiredService<IPluginInfoQuery>();

        public override object? Create(Type pluginType)
        {
            var ctor = pluginType.GetConstructor(new [] {typeof(IPluginInfoQuery)});
            if (ctor == null)
                return null;
            return ctor.Invoke(new object[] {Query});
        }
    }
}
