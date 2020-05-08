using System.Collections.Generic;
using Stl.Text;

namespace Stl.Fusion.Channels
{
    public static class ChannelRegistryEx
    {
        public static IChannel<TMessage> Get<TMessage>(this IChannelRegistry<TMessage> registry, Symbol channelId) 
            => registry.TryGet(channelId) ?? throw new KeyNotFoundException();
    }
}
