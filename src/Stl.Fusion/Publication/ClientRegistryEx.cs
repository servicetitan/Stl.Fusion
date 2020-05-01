using System.Collections.Generic;
using Stl.Text;

namespace Stl.Fusion.Publication
{
    public static class ClientRegistryEx
    {
        public static IClient Get(this IClientRegistry registry, Symbol clientKey) 
            => registry.TryGet(clientKey) ?? throw new KeyNotFoundException();
    }
}
