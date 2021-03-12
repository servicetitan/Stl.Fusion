using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Fusion.Bridge.Messages;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public interface IChannelProvider
    {
        Task<Channel<BridgeMessage>> CreateChannel(Symbol publisherId, CancellationToken cancellationToken);
    }
}
