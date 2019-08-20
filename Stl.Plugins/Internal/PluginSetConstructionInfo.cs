using System;
using System.Collections.Generic;
using System.Reflection;

namespace Stl.Plugins.Internal
{
    public class PluginSetConstructionInfo
    {
        public Type[]? Plugins { get; set; }
        public Assembly[]? Assemblies { get; set; }
        public Dictionary<Assembly, HashSet<Assembly>>? AssemblyDependencies { get; set; }  
        public IPluginFactory? TemporaryPluginFactory { get; set; }
    }
}
