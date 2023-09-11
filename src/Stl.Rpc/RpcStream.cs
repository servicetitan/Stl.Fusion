using System.Diagnostics;
using Stl.Interception;
using Stl.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

#pragma warning disable MA0055

[DataContract]
public abstract partial class RpcStream : IRpcObject
{
    protected static readonly UnboundedChannelOptions RemoteChannelOptions = new() {
        SingleReader = true,
        SingleWriter = true,
        AllowSynchronousContinuations = false, // We don't want sync handlers to "clog" the call processing loop
    };

    public static RpcStream<T> New<T>(IAsyncEnumerable<T> outgoingSource)
        => new(outgoingSource);
    public static RpcStream<T> New<T>(IEnumerable<T> outgoingSource)
        => new(outgoingSource.ToAsyncEnumerable());

    [DataMember, MemoryPackOrder(1)]
    public int AckDistance { get; init; } = 500;
    [DataMember, MemoryPackOrder(2)]
    public int AdvanceDistance { get; init; } = 1001;

    // Non-serialized members
    [JsonIgnore, MemoryPackIgnore] public long Id { get; protected set; }
    [JsonIgnore, MemoryPackIgnore] public RpcPeer? Peer { get; protected set; }
    [JsonIgnore, MemoryPackIgnore] public abstract Type ItemType { get; }
    [JsonIgnore, MemoryPackIgnore] public abstract RpcObjectKind Kind { get; }

    public override string ToString()
        => $"{GetType().GetName()}(#{Id} @ {Peer?.Ref}, {Kind})";

    Task IRpcObject.OnReconnected(CancellationToken cancellationToken)
        => OnReconnected(cancellationToken);

    void IRpcObject.OnMissing()
        => OnMissing();

    // Protected methods

    protected internal abstract ArgumentList CreateStreamItemArguments();
    protected internal abstract Task OnItem(long index, long ackIndex, object? item);
    protected internal abstract Task OnEnd(long index, Exception? error);
    protected abstract Task OnReconnected(CancellationToken cancellationToken);
    protected abstract void OnMissing();
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class RpcStream<T> : RpcStream, IAsyncEnumerable<T>
{
    private long _nextIndex;
    private readonly IAsyncEnumerable<T>? _localSource;
    private Channel<T>? _remoteChannel;
    private bool _isCloseAckSent;
    private bool _isRemoteObjectRegistered;
    private readonly object _lock;

    [DataMember, MemoryPackOrder(0)]
    public long SerializedId {
        get {
            // This member must be never accessed directly - its only purpose is to be called on serialization
            this.RequireKind(RpcObjectKind.Local);
            Peer = RpcOutboundContext.Current?.Peer ?? RpcInboundContext.GetCurrent().Peer;
            var sharedObjects = Peer.SharedObjects;
            Id = sharedObjects.NextId(); // NOTE: Id changes on serialization!
            var sharedStream = new RpcSharedStream<T>(this);
            sharedObjects.Register(sharedStream);
            return Id;
        }
        set {
            lock (_lock) {
                this.RequireKind(RpcObjectKind.Remote);
                Id = value;
                Peer = RpcInboundContext.GetCurrent().Peer;
                _isRemoteObjectRegistered = true;
                Peer.RemoteObjects.Register(this);
            }
        }
    }

    [JsonIgnore] public override Type ItemType => typeof(T);
    [JsonIgnore] public override RpcObjectKind Kind
        => _localSource != null ? RpcObjectKind.Local : RpcObjectKind.Remote;

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public RpcStream()
        => _lock = new();

    public RpcStream(IAsyncEnumerable<T> localSource)
    {
        _localSource = localSource;
        _lock = null!; // Must not be used for local streams
    }

    ~RpcStream()
    {
        if (_lock != null!)
            CloseRemoteChannel(null, true);
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        if (_localSource != null)
            return _localSource.GetAsyncEnumerator(cancellationToken);

        lock (_lock) {
            if (_remoteChannel != null)
                throw Internal.Errors.RemoteRpcStreamCanBeEnumeratedJustOnce();

            _remoteChannel = Channel.CreateUnbounded<T>(RemoteChannelOptions);
            if (_nextIndex < 0) // Marked as missing
                _remoteChannel.Writer.TryComplete(Internal.Errors.RpcStreamNotFound());
            return new RemoteChannelEnumerator(this, cancellationToken);
        }
    }

    // Protected methods

    internal IAsyncEnumerable<T> GetLocalSource()
    {
        this.RequireKind(RpcObjectKind.Local);
        return _localSource!;
    }

    protected internal override ArgumentList CreateStreamItemArguments()
        => ArgumentList.New(0L, 0L, default(T));

    protected internal override Task OnItem(long index, long ackIndex, object? item)
    {
        lock (_lock) {
            if (_remoteChannel == null || index < _nextIndex)
                return Task.CompletedTask;

            // Debug.WriteLine($"Got item: {index}, {ackIndex}");
            if (index > _nextIndex)
                return SendAck(_nextIndex, true);

            _nextIndex++;
            _remoteChannel.Writer.TryWrite((T)item!); // Must always succeed for unbounded channel
            var delta = _nextIndex - ackIndex;
            if (delta >= 0 && delta % AckDistance == 0)
                return SendAck(_nextIndex);

            return Task.CompletedTask;
        }
    }

    protected internal override Task OnEnd(long index, Exception? error)
    {
        lock (_lock) {
            if (_remoteChannel == null || index < _nextIndex)
                return Task.CompletedTask;

            if (index > _nextIndex)
                return SendAck(_nextIndex, true);

            _nextIndex++;
            return CloseRemoteChannelUnsafe(error);
        }
    }

    protected override Task OnReconnected(CancellationToken cancellationToken)
    {
        lock (_lock) {
            if (_remoteChannel == null)
                return Task.CompletedTask;

            return _isCloseAckSent
                ? SendCloseAck()
                : SendAck(_nextIndex, true);
        }
    }

    protected override void OnMissing()
    {
        lock (_lock) {
            _nextIndex = -1;
            _remoteChannel?.Writer.TryComplete(Internal.Errors.RpcStreamNotFound());
        }
    }

    // Private methods

    private Task CloseRemoteChannel(Exception? error, bool isFinalizing = false)
    {
        lock (_lock)
            return CloseRemoteChannelUnsafe(error, isFinalizing);
    }

    private Task CloseRemoteChannelUnsafe(Exception? error, bool isFinalizing = false)
    {
        var result = Task.CompletedTask;
        if (_remoteChannel != null) {
            if (!_isCloseAckSent) {
                _isCloseAckSent = true;
                result = SendCloseAck();
            }
            // ReSharper disable once InconsistentlySynchronizedField
            if (!isFinalizing)
                _remoteChannel.Writer.TryComplete(error);
        }
        if (_isRemoteObjectRegistered) {
            _isRemoteObjectRegistered = false;
            Peer?.RemoteObjects.Unregister(this);
        }
        return result;
    }

    private Task SendCloseAck()
        => SendAck(long.MaxValue);

    private Task SendAck(long index, bool mustReset = false)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (_nextIndex < 0) // Marked as missing
            return Task.CompletedTask;

        Debug.WriteLine($"ACK: ({index}, {mustReset})");
        var peer = Peer!;
        return peer.Hub.SystemCallSender.StreamAck(peer, Id, index, mustReset);
    }

    // Nested types

    private class RemoteChannelEnumerator(RpcStream<T> stream, CancellationToken cancellationToken) : IAsyncEnumerator<T>
    {
        private readonly ChannelReader<T> _reader =  stream._remoteChannel!.Reader;
        private bool _isStarted;
        private bool _isEnded;
        private Result<T> _current;

        public T Current => _isStarted
            ? _current.Value
            : throw new InvalidOperationException($"{nameof(MoveNextAsync)} should be called first.");

        public ValueTask DisposeAsync()
            => stream.CloseRemoteChannel(null).ToValueTask();

        public ValueTask<bool> MoveNextAsync()
        {
            if (_isEnded)
                return default;

            try {
                if (_reader.TryRead(out var current))
                    _current = current;
                else
                    return MoveNext();
            }
            catch (Exception e) {
                _current = Result.Error<T>(e);
                _isEnded = true;
            }
            return ValueTaskExt.TrueTask;

            async ValueTask<bool> MoveNext()
            {
                try {
                    if (!_isStarted) {
                        _isStarted = true;
                        await stream.SendAck(0).ConfigureAwait(false);
                    }
                    if (!await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        _isEnded = true;
                        return false;
                    }
                    if (!_reader.TryRead(out var current1))
                        throw Errors.InternalError("Couldn't read after successful WaitToReadAsync call.");

                    _current = current1;
                    return true;
                }
                catch (Exception e) {
                    _current = Result.Error<T>(e);
                    _isEnded = true;
                    return true;
                }
            }
        }
    }
}
