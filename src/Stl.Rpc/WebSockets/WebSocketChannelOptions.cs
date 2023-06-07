namespace Stl.Rpc.WebSockets;

public record WebSocketChannelOptions
{
    public static WebSocketChannelOptions Default { get; set; } = new();

    public BoundedChannelOptions ReadChannelOptions { get; init; } = new(16) {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = true,
        AllowSynchronousContinuations = true,
    };
    public BoundedChannelOptions WriteChannelOptions { get; init; } = new(16) {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = true,
    };
}
