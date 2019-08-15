using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Stl.Plugins.Metadata;
using Stl.Reflection;

namespace Stl.Plugins 
{
    public interface IPluginEnumerator
    {
        PluginSetInfo GetPluginSetInfo();
    }

    public class PluginEnumerator : IPluginEnumerator
    {
        public Regex AssemblyNamePattern { get; set; } = new Regex(".*\\.dll");
        public HashSet<TypeRef> PluginTypes { get; } = new HashSet<TypeRef>();

        public virtual PluginSetInfo GetPluginSetInfo()
        {
            throw new NotImplementedException();
        }

        public PluginEnumerator AddPluginTypes(params TypeRef[] types)
        {
            foreach (var type in types)
                AddPluginType(type);
            return this;
        }

        public PluginEnumerator AddPluginType<TPlugin>()
            => AddPluginType(typeof(TPlugin));

        public PluginEnumerator AddPluginType(TypeRef type)
        {
            PluginTypes.Add(type);
            return this;
        }

        public PluginEnumerator SetAssemblyNamePattern(Regex assemblyNamePattern)
        {
            AssemblyNamePattern = assemblyNamePattern;
            return this;
        }
    }
}
