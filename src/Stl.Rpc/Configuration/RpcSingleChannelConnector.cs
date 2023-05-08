using Stl.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcSingleChannelConnector : RpcConnector
{
    private readonly TaskCompletionSource<Channel<RpcMessage>> _channelSource = TaskCompletionSourceExt.New<Channel<RpcMessage>>();

    public Channel<RpcMessage> Channel {
        get {
            var channelTask = _channelSource.Task;
            if (!channelTask.IsCompleted)
                throw Errors.NotInitialized(nameof(Channel));

#pragma warning disable VSTHRD104
            return channelTask.Result;
#pragma warning restore VSTHRD104
        }
        set => _channelSource.TrySetResult(value);
    }

    public override Task<Channel<RpcMessage>> Connect(RpcPeer peer, CancellationToken cancellationToken)
        => _channelSource.Task;
}
