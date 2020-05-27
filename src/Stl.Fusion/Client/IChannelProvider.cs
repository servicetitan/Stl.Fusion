using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Fusion.Bridge.Messages;
using Stl.Text;

namespace Stl.Fusion.Client
{
    public interface IChannelProvider
    {
        Task<Channel<Message>> CreateChannelAsync(Symbol publisherId, CancellationToken cancellationToken); 
    }
}
