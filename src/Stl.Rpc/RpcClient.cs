using Stl.Rpc.Infrastructure;
using Stl.Rpc.WebSockets;

namespace Stl.Rpc;

public class RpcClient
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public string RequestPath { get; init; } = "/rpc/ws";
        public string ClientIdParameterName { get; init; } = "clientId";
        public WebSocketChannelOptions WebSocketChannelOptions { get; init; } = WebSocketChannelOptions.Default;
        public Func<RpcClientPeer, string> AddressResolver { get; init; } = peer => peer.Name.Value.TrimSuffix("/");
    }

    private ILogger? _log;
    private RpcHub? _rpcHub;

    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public Options Settings { get; }
    public IServiceProvider Services { get; }
    public RpcHub RpcHub => _rpcHub ??= Services.GetRequiredService<RpcHub>();

    public RpcClient(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
    }

    public Task<Channel<RpcMessage>> GetChannel(RpcClientPeer peer, CancellationToken cancellationToken)
    {
#pragma warning disable MA0025
        throw new NotImplementedException();
#pragma warning restore MA0025
    }
}
