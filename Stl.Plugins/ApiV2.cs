using System;
using System.Collections.Generic;

namespace Stl.Plugins
{
    public interface IExtension
    {
        
    }
    
    public interface IPlugin2 : IServiceProvider, IDisposable
    {
        IEnumerable<Type> Extensions { get; }
    }
    
    public interface IPluginHost2 : IServiceProvider, IDisposable
    {
        IEnumerable<string> PluginTypeNames { get; }
    }

}
