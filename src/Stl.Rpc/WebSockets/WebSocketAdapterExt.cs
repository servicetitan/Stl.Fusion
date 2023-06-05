using Stl.Channels;

namespace Stl.Rpc.WebSockets;

public static class WebSocketAdapterExt
{
    public static BoundedChannelOptions DefaultReadChannelOptions { get; set; } = new(16) {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = true,
        AllowSynchronousContinuations = true,
    };
    public static BoundedChannelOptions DefaultWriteChannelOptions { get; set; } = new(16) {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = true,
    };

    public static Channel<T> ToChannel<T>(this WebSocketAdapter<T> webSocket, CancellationToken cancellationToken = default)
    {
        var readChannel = Channel.CreateBounded<T>(DefaultReadChannelOptions);
        var writeChannel = Channel.CreateBounded<T>(DefaultWriteChannelOptions);
        return webSocket.ToChannel(readChannel, writeChannel, cancellationToken);
    }

    public static Channel<T> ToChannel<T>(
        this WebSocketAdapter<T> webSocket,
        Channel<T> readChannel,
        Channel<T> writeChannel,
        CancellationToken cancellationToken = default)
    {
        var stopTokenSource = cancellationToken.CreateLinkedTokenSource();
        var stopToken = stopTokenSource.Token;

        using var suppressFlow = ExecutionContextExt.SuppressFlow();

        // Copy ReadAll(...) items to ReadChannel
        _ = Task.Run(async () => {
            var error = (Exception?) null;
            var readChannelWriter = readChannel.Writer;
            try {
                await foreach (var item in webSocket.ReadAll(stopToken).ConfigureAwait(false))
                    await readChannelWriter.WriteAsync(item, stopToken).ConfigureAwait(false);
            }
            catch (Exception e) {
                error = e;
            }
            finally {
                stopTokenSource.CancelAndDisposeSilently();
                readChannelWriter.TryComplete(error);
                await webSocket.Close(error).ConfigureAwait(false);
            }
        }, default);

        // Copy WriteChannel items to Write(...)
        _ = Task.Run(async () => {
            try {
                await foreach (var item in writeChannel.Reader.ReadAllAsync(stopToken).ConfigureAwait(false))
                    await webSocket.Write(item, stopToken).ConfigureAwait(false);
            }
            finally {
                stopTokenSource.CancelAndDisposeSilently();
            }
        }, default);

        var channel = new CustomChannel<T>(readChannel, writeChannel);
        return channel;
    }
}
