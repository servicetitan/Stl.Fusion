using System.Threading;
using System.Threading.Tasks;
using Stl.Text;

namespace Stl.Fusion.Channels
{
    public class PassThroughChannel<TMessage> : ChannelBase<TMessage>
    {
        public IChannel<TMessage> Counterpart { get; set; }

        public PassThroughChannel(Symbol id, IChannel<TMessage>? counterpart = null) : base(id) 
            => Counterpart = counterpart ?? NullChannel<TMessage>.Instance;

        public override Task SendAsync(TMessage message, CancellationToken cancellationToken) 
            => throw new System.NotImplementedException();
    }
}
