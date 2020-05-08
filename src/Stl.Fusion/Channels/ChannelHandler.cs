using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Channels
{
    public interface IChannelHandler<TMessage>
    {
        Task OnMessageReceivedAsync(IChannel<TMessage> channel, TMessage message, CancellationToken cancellationToken);
        void OnDisconnected(IChannel<TMessage> channel);
    }

    public abstract class ChannelHandlerBase<TMessage> : IChannelHandler<TMessage>
    {
        public abstract Task OnMessageReceivedAsync(IChannel<TMessage> channel, TMessage message, CancellationToken cancellationToken);
        public abstract void OnDisconnected(IChannel<TMessage> channel);
    }

    public class ChannelHandler<TMessage> : ChannelHandlerBase<TMessage>
    {
        private readonly Func<IChannel<TMessage>, TMessage, CancellationToken, Task> _messageReceived;
        private readonly Action<IChannel<TMessage>> _disconnected;

        public ChannelHandler(
            Func<IChannel<TMessage>, TMessage, CancellationToken, Task> messageReceived, 
            Action<IChannel<TMessage>> disconnected)
        {
            _messageReceived = messageReceived ?? throw new ArgumentNullException(nameof(messageReceived));
            _disconnected = disconnected ?? throw new ArgumentNullException(nameof(disconnected));
        }

        public override Task OnMessageReceivedAsync(IChannel<TMessage> channel, TMessage message, CancellationToken cancellationToken) 
            => _messageReceived.Invoke(channel, message, cancellationToken);

        public override void OnDisconnected(IChannel<TMessage> channel)
        {
            channel.RemoveHandler(this);
            _disconnected.Invoke(channel);
        }
    }
}
