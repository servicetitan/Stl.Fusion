using System.Diagnostics;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcSharedStream(RpcStream stream) : WorkerBase, IRpcSharedObject
{
    protected static readonly Exception NoError = new();

    private ILogger? _log;
    private long _lastKeepAliveAt = CpuTimestamp.Now.Value;

    protected ILogger Log => _log ??= Peer.Hub.Services.LogFor(GetType());

    public long Id { get; } = stream.Id;
    public RpcObjectKind Kind { get; } = stream.Kind;
    public RpcStream Stream { get; } = stream;
    public RpcPeer Peer { get; } = stream.Peer!;
    public CpuTimestamp LastKeepAliveAt {
        get => new(Interlocked.Read(ref _lastKeepAliveAt));
        set => Interlocked.Exchange(ref _lastKeepAliveAt, value.Value);
    }

    Task IRpcObject.OnReconnected(CancellationToken cancellationToken)
        => throw Stl.Internal.Errors.InternalError(
            $"This method should never be called on {nameof(RpcSharedStream)}.");

    void IRpcObject.OnMissing()
        => throw Stl.Internal.Errors.InternalError(
            $"This method should never be called on {nameof(RpcSharedStream)}.");

    public void OnKeepAlive()
        => LastKeepAliveAt = CpuTimestamp.Now;

    public abstract void OnAck(long nextIndex, bool mustReset);
}

public sealed class RpcSharedStream<T>(RpcStream stream) : RpcSharedStream(stream)
{
    private readonly RpcSystemCallSender _systemCallSender = stream.Peer!.Hub.SystemCallSender;
    private readonly Channel<(long NextIndex, bool MustReset)> _acks = Channel.CreateUnbounded<(long, bool)>(
        new() {
            SingleReader = true,
            SingleWriter = true,
        });

    private long _lastAckIndex;

    public new RpcStream<T> Stream { get; } = (RpcStream<T>)stream;

    protected override async Task DisposeAsyncCore()
    {
        _acks.Writer.TryWrite((long.MaxValue, false)); // Just in case
        try {
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
        finally {
            Peer.SharedObjects.Unregister(this);
        }
    }

    public override void OnAck(long nextIndex, bool mustReset)
    {
        LastKeepAliveAt = CpuTimestamp.Now;
        if (WhenRunning == null)
            this.Start();
        _acks.Writer.TryWrite((nextIndex, mustReset)); // Must always succeed for unbounded channel
    }

    // Protected & private methods

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        IAsyncEnumerator<T>? enumerator = null;
        try {
            enumerator = Stream.GetLocalSource().GetAsyncEnumerator(cancellationToken);
            var isEnumerationEnded = false;
            var ackReader = _acks.Reader;
            var buffer = new RingBuffer<Result<T>>(Stream.AdvanceDistance + 1);
            var bufferOffset = 0L;
            var index = 0L;
            var nextAckTask = ackReader.ReadAsync(cancellationToken);
            while (true) {
                nextAck:
                // 1. Await for acknowledgement
                (long NextIndex, bool MustReset) ack;
                if (nextAckTask.IsCompleted)
                    ack = nextAckTask.Result;
                else {
                    // Debug.WriteLine("-> Waiting for ACK");
                    ack = await nextAckTask.ConfigureAwait(false);
                }
                // Debug.WriteLine($"-> ACK: {ack}");
                if (ack.NextIndex == long.MaxValue)
                    return; // The only point we exit is when the client tells us it's done

                nextAckTask = ackReader.ReadAsync(cancellationToken);
                var ackIndex = ack.NextIndex + Stream.AckDistance;
                var maxIndex = ack.NextIndex + Stream.AdvanceDistance;
                if (ack.MustReset)
                    index = ack.NextIndex;

                // 2. Send as much as we can until we'll need to await for the next acknowledgement
                while (index < maxIndex) {
                    var bufferIndex = index - bufferOffset;
                    if (bufferIndex < 0L) {
                        // The requested item is somewhere before the buffer start position
                        await SendInvalidPosition(index, ackIndex).ConfigureAwait(false);
                        break;
                    }

                    if (nextAckTask.IsCompleted)
                        break; // Got Ack, must restart

                    Result<T> item;
                    var missingItemCount = 1 + bufferIndex - buffer.Count;
                    if (missingItemCount > 0) {
                        // Fill buffer loop
                        while (missingItemCount-- > 0) {
                            if (isEnumerationEnded) {
                                // index > last item index, which means we've sent StreamEnd already
                                await SendInvalidPosition(index, ackIndex).ConfigureAwait(false);
                                goto nextAck;
                            }

                            try {
                                var canMove = await enumerator.MoveNextAsync().ConfigureAwait(false);
                                if (canMove)
                                    item = enumerator.Current;
                                else
                                    item = Result.Error<T>(NoError);
                            }
                            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                                item = Result.Error<T>(Errors.RpcStreamNotFound());
                            }
                            catch (Exception e) {
                                item = Result.Error<T>(e);
                            }

                            if (buffer.IsFull) {
                                buffer.PullHead();
                                bufferOffset++;
                            }
                            buffer.PushTail(item);
                            isEnumerationEnded |= item.HasError;
                        }
                        // Some items were buffered, let's retry sending them
                        continue;
                    }

                    item = buffer[(int)bufferIndex];
                    await Send(index++, ackIndex, item).ConfigureAwait(false);
                    if (item.HasError)
                        break; // It's the last item -> all we can do now is to wait for Ack
                }
            }
        }
        finally {
            _ = DisposeAsync();
            _ = enumerator?.DisposeAsync();
        }
    }

    private Task SendInvalidPosition(long index, long ackIndex)
        => Send(index, ackIndex, Result.Error<T>(Errors.RpcStreamInvalidPosition()));

    private Task Send(long index, long ackIndex, Result<T> item)
    {
        // Debug.WriteLine($"Sent item: {index}, {ackIndex}");
        _lastAckIndex = ackIndex;
        if (item.IsValue(out var value))
            return _systemCallSender.StreamItem(Peer, Id, index, ackIndex, value);

        var error = ReferenceEquals(item.Error, NoError) ? null : item.Error;
        return _systemCallSender.StreamEnd(Peer, Id, index, error);
    }
}
