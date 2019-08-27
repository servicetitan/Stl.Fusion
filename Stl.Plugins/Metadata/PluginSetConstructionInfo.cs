using System;
using System.Collections.Generic;
using System.Reflection;
using Stl.Plugins.Services;

namespace Stl.Plugins.Metadata
{
    public class PluginSetConstructionInfo
    {
        public Type[]? Plugins { get; set; }
        public Assembly[]? Assemblies { get; set; }
        public Dictionary<Assembly, HashSet<Assembly>>? AllAssemblyRefs { get; set; }  
        public IPluginFactory? TemporaryPluginFactory { get; set; }
    }
}
