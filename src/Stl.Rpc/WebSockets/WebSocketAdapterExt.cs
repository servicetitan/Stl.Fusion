using Stl.Channels;

namespace Stl.Rpc.WebSockets;

public static class WebSocketAdapterExt
{
    public static Channel<T> ToChannel<T>(
        this WebSocketAdapter<T> webSocket,
        WebSocketChannelOptions options,
        CancellationToken cancellationToken = default)
    {
        var readChannel = Channel.CreateBounded<T>(options.ReadChannelOptions);
        var writeChannel = Channel.CreateBounded<T>(options.WriteChannelOptions);
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
        var isClosedGracefully = false;

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
                if (isClosedGracefully)
                    error = null;
                readChannelWriter.TryComplete(error);
                await webSocket.Close(error).ConfigureAwait(false);
            }
        }, default);

        // Copy WriteChannel items to Write(...)
        _ = Task.Run(async () => {
            try {
                await foreach (var item in writeChannel.Reader.ReadAllAsync(stopToken).ConfigureAwait(false))
                    await webSocket.Write(item, stopToken).ConfigureAwait(false);
                isClosedGracefully = true;
            }
            finally {
                stopTokenSource.CancelAndDisposeSilently();
            }
        }, default);

        var channel = new CustomChannel<T>(readChannel, writeChannel);
        return channel;
    }
}
