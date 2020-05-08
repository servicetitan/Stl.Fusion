using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Text;

namespace Stl.Fusion.Channels
{
    public class NullChannel<TMessage> : IChannel<TMessage>
    {
        public static readonly NullChannel<TMessage> Instance = new NullChannel<TMessage>();

        public Symbol Id { get; } = $"null://./{typeof(TMessage).FullName}";

        private NullChannel() { }

        public ValueTask DisposeAsync() 
            => ValueTaskEx.CompletedTask;

        public Task SendAsync(TMessage message, CancellationToken cancellationToken) 
            => Task.CompletedTask;

        public bool AddHandler(IChannelHandler<TMessage> handler) => true; 
        public bool RemoveHandler(IChannelHandler<TMessage> handler) => true;
    }
}
