using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcConnection
{
    public Channel<RpcMessage> Channel { get; }
    public ImmutableOptionSet Options { get; set; }

    public RpcConnection(Channel<RpcMessage> channel)
        : this(channel, ImmutableOptionSet.Empty)
    { }

    public RpcConnection(Channel<RpcMessage> channel, ImmutableOptionSet options)
    {
        Channel = channel;
        Options = options;
    }
}
