using System;
using System.Collections.Generic;
using System.Reflection;
using Stl.Plugins.Services;

namespace Stl.Plugins.Metadata
{
    public class PluginSetConstructionInfo
    {
        public Type[] Plugins { get; set; } = null!;
        public Assembly[] Assemblies { get; set; } = null!;
        public Dictionary<Assembly, HashSet<Assembly>> AllAssemblyRefs { get; set; } = null!; 
        public IPluginFactory PluginFactory { get; set; } = null!;
    }
}
