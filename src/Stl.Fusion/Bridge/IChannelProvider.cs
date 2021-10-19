using Stl.Fusion.Bridge.Messages;

namespace Stl.Fusion.Bridge;

public interface IChannelProvider
{
    Task<Channel<BridgeMessage>> CreateChannel(Symbol publisherId, CancellationToken cancellationToken);
}
