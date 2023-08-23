using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcConnection(Channel<RpcMessage> channel, ImmutableOptionSet options)
{
    public Channel<RpcMessage> Channel { get; } = channel;
    public ImmutableOptionSet Options { get; set; } = options;

    public RpcConnection(Channel<RpcMessage> channel)
        : this(channel, ImmutableOptionSet.Empty)
    { }
}
