using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcSharedStream(RpcStream stream) : WorkerBase, IRpcSharedObject
{
    protected static readonly Exception NoError = new();

    private long _lastKeepAliveAt = CpuTimestamp.Now.Value;

    public long Id { get; } = stream.Id;
    public RpcObjectKind Kind { get; } = stream.Kind;
    public RpcStream Stream { get; } = stream;
    public RpcPeer Peer { get; } = stream.Peer!;
    public CpuTimestamp LastKeepAliveAt {
        get => new(Interlocked.Read(ref _lastKeepAliveAt));
        set => Interlocked.Exchange(ref _lastKeepAliveAt, value.Value);
    }

    ValueTask IRpcObject.OnReconnected(CancellationToken cancellationToken)
        => throw Stl.Internal.Errors.InternalError(
            $"This method should never be called on {nameof(RpcSharedStream)}.");

    void IRpcObject.OnMissing()
        => throw Stl.Internal.Errors.InternalError(
            $"This method should never be called on {nameof(RpcSharedStream)}.");

    public void OnKeepAlive()
        => LastKeepAliveAt = CpuTimestamp.Now;

    public abstract void OnAck(long nextIndex, bool mustReset);

    // Protected methods

    protected override Task DisposeAsyncCore()
        => base.DisposeAsyncCore()
            .ContinueWith(_ => Peer.SharedObjects.Unregister(this), TaskScheduler.Default);
}

public sealed class RpcSharedStream<T>(RpcStream stream) : RpcSharedStream(stream)
{
    private readonly Channel<Result<T>> _streamBuffer = Channel.CreateBounded<Result<T>>(
        new BoundedChannelOptions(stream.AckInterval * 4) {
            SingleReader = true,
            SingleWriter = true,
        });
    private readonly Channel<(long NextIndex, bool MustReset)> _acks = Channel.CreateUnbounded<(long, bool)>(
        new() {
            SingleReader = true,
            SingleWriter = true,
        });
    private readonly RpcSystemCallSender _systemCallSender = stream.Peer!.Hub.SystemCallSender;

    public new RpcStream<T> Stream { get; } = (RpcStream<T>)stream;

    public override void OnAck(long nextIndex, bool mustReset)
    {
        LastKeepAliveAt = CpuTimestamp.Now;
        if (WhenRunning == null)
            this.Start();
        _acks.Writer.TryWrite((nextIndex, mustReset)); // Must always succeed for unbounded channel
    }

    // Protected & private methods

    protected override Task OnRun(CancellationToken cancellationToken)
    {
        var fillStreamBufferTask = FillStreamBuffer(cancellationToken);
        var sendStreamTask = SendStream(cancellationToken)
            .ContinueWith(_ => Dispose(), TaskScheduler.Default);
        return Task.WhenAll(fillStreamBufferTask, sendStreamTask);
    }

    private async Task FillStreamBuffer(CancellationToken cancellationToken)
    {
        var localSource = Stream.GetLocalSource().WithCancellation(cancellationToken);
        var writer = _streamBuffer.Writer;
        var error = NoError;
        try {
            await foreach (var item in localSource.ConfigureAwait(false))
                await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            error = Errors.RpcStreamNotFound();
        }
        catch (Exception e) {
            error = e;
        }
        try {
            if (!cancellationToken.IsCancellationRequested)
                await writer.WriteAsync(Result.Error<T>(error), cancellationToken).ConfigureAwait(false);
        }
        finally {
            writer.Complete();
        }
    }

    private async Task SendStream(CancellationToken cancellationToken)
    {
        var ackReader = _acks.Reader;
        var streamBufferReader = _streamBuffer.Reader;
        var buffer = new RingBuffer<Result<T>>(Stream.AckInterval * 3);
        var isBufferCompleted = false;
        var bufferOffset = 0L;
        var index = 0L;
        while (true) {
            // 1. Await for acknowledgement
            var ack = await ackReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (ack.NextIndex == long.MaxValue)
                return; // The only point we exit is when the client tells us it's done

            if (ack.MustReset)
                index = ack.NextIndex;

            // 2. Send as much as we can until we'll need to await for the next acknowledgement
            var ackIndex = index + Stream.AckInterval;
            var maxIndex = ackIndex + Stream.AckInterval;
            while (index < maxIndex) {
                var bufferIndex = index - bufferOffset;
                if (bufferIndex < 0L) {
                    // The requested item is somewhere before the buffer start position
                    await Send(index, ackIndex, Result.Error<T>(Errors.RpcStreamInvalidPosition())).ConfigureAwait(false);
                    break;
                }

                Result<T> item;
                if (bufferIndex < buffer.Count) {
                    item = buffer[(int)bufferIndex];
                    await Send(index, ackIndex, item).ConfigureAwait(false);
                    if (item.HasError)
                        break;

                    index++;
                    continue;
                }

                if (isBufferCompleted)
                    break;

                if (!streamBufferReader.TryRead(out item)) {
                    if (!await streamBufferReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                        item = Result.Error<T>(Errors.RpcStreamNotFound());
                    else
                        streamBufferReader.TryRead(out item);
                }

                // Must buffer at least one more item
                if (buffer.IsFull) {
                    buffer.PullHead();
                    bufferOffset++;
                }
                buffer.PushTail(item);
                isBufferCompleted |= item.HasError;
                // We'll send it on the next loop iteration
            }
        }
    }

    private ValueTask Send(long index, long ackIndex, Result<T> item)
    {
        if (item.IsValue(out var value))
            return _systemCallSender.StreamItem(Peer, Id, index, ackIndex, value);

        var error = ReferenceEquals(item.Error, NoError) ? null : item.Error;
        return _systemCallSender.StreamEnd(Peer, Id, index, error);
    }
}
