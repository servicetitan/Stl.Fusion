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
    public int AckInterval { get; init; } = 128;

    // Non-serialized members
    [JsonIgnore, MemoryPackIgnore] public long Id { get; protected set; }
    [JsonIgnore, MemoryPackIgnore] public RpcPeer? Peer { get; protected set; }
    [JsonIgnore, MemoryPackIgnore] public abstract Type ItemType { get; }
    [JsonIgnore, MemoryPackIgnore] public abstract RpcObjectKind Kind { get; }

    public override string ToString()
        => $"{GetType().GetName()}(#{Id} @ {Peer?.Ref}, {Kind})";

    ValueTask IRpcObject.OnReconnected(CancellationToken cancellationToken)
        => OnReconnected(cancellationToken);

    void IRpcObject.OnMissing()
        => OnMissing();

    // Protected methods

    protected internal abstract ArgumentList CreateStreamItemArguments();
    protected internal abstract void OnItem(long index, long ackIndex, object? item);
    protected internal abstract void OnEnd(long index, Exception? error);
    protected abstract ValueTask OnReconnected(CancellationToken cancellationToken);
    protected abstract void OnMissing();
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class RpcStream<T> : RpcStream, IAsyncEnumerable<T>
{
    private long _nextIndex;
    private readonly IAsyncEnumerable<T>? _localSource;
    private Channel<T>? _remoteChannel;
    private bool _isCloseAckSent;
    private bool _isUnregistered;
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
            this.RequireKind(RpcObjectKind.Remote);
            Id = value;
            Peer = RpcInboundContext.GetCurrent().Peer;
            Peer.RemoteObjects.Register(this);
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
        if (_localSource == null)
            TryComplete(null, true);
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

    protected internal override void OnItem(long index, long ackIndex, object? item)
    {
        lock (_lock) {
            if (_remoteChannel == null)
                throw Errors.InternalError("RpcStream got an item before the enumeration.");

            // Debug.WriteLine($"Got item: {index}, {ackIndex}");
            if (index < _nextIndex)
                return;

            if (index > _nextIndex) {
                _ = SendAck(_nextIndex, true);
                return;
            }

            _nextIndex++;
            _remoteChannel.Writer.TryWrite((T)item!); // Must always succeed for unbounded channel
            var delta = _nextIndex - ackIndex;
            if (delta < 0 || delta % AckInterval != 0)
                return;

            _ = SendAck(ackIndex);
        }
    }

    protected internal override void OnEnd(long index, Exception? error)
    {
        lock (_lock) {
            if (_remoteChannel == null)
                throw Errors.InternalError("RpcStream got an item before the enumeration.");

            if (index < _nextIndex)
                return;

            if (index > _nextIndex) {
                _ = SendAck(_nextIndex, true);
                return;
            }

            _nextIndex++;
            TryCompleteUnsafe(error);
        }
    }

    protected override ValueTask OnReconnected(CancellationToken cancellationToken)
    {
        lock (_lock) {
            if (_remoteChannel == null)
                return default;

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

    private void TryComplete(Exception? error, bool isFinalizing = false)
    {
        lock (_lock)
            TryCompleteUnsafe(error, isFinalizing);
    }

    private void TryCompleteUnsafe(Exception? error, bool isFinalizing = false)
    {
        if (_remoteChannel != null) {
            if (!_isCloseAckSent) {
                _isCloseAckSent = true;
                _ = SendCloseAck();
            }
            // ReSharper disable once InconsistentlySynchronizedField
            if (!isFinalizing)
                _remoteChannel.Writer.TryComplete(error);
        }
        if (_localSource == null && !_isUnregistered) {
            _isUnregistered = true;
            Peer?.RemoteObjects.Unregister(this);
        }
    }

    private ValueTask SendCloseAck()
        => SendAck(long.MaxValue);

    private ValueTask SendAck(long index, bool mustReset = false)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (_nextIndex < 0) // Marked as missing
            return default;

        // Debug.WriteLine($"ACK: ({index}, {mustReset})");
        var peer = Peer!;
        return peer.Hub.SystemCallSender.StreamAck(peer, Id, index, mustReset);
    }

    // Nested types

    private class RemoteChannelEnumerator(RpcStream<T> stream, CancellationToken cancellationToken) : IAsyncEnumerator<T>
    {
        private readonly ChannelReader<T> _reader =  stream._remoteChannel!.Reader;
        private bool _isStarted;
        private bool _isCompleted;
        private Result<T> _current;

        public T Current => _isStarted
            ? _current.Value
            : throw new InvalidOperationException($"{nameof(MoveNextAsync)} should be called first.");

        public ValueTask DisposeAsync()
        {
            if (!_isCompleted) {
                _isCompleted = true;
                stream.TryComplete(null);
            }
            return default;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            if (_isCompleted)
                return default;
            if (!_reader.TryRead(out var current))
                return Move();

            _current = current;
            return ValueTaskExt.TrueTask;

            async ValueTask<bool> Move()
            {
                try {
                    if (!_isStarted) {
                        _isStarted = true;
                        await stream.SendAck(0).ConfigureAwait(false);
                    }
                    while (true) {
                        if (!await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                            _isCompleted = true;
                            return false;
                        }

                        if (_reader.TryRead(out var current1)) {
                            _current = current1;
                            return true;
                        }
                    }
                }
                catch (Exception e) {
                    _current = Result.Error<T>(e);
                    _isCompleted = true;
                    return true;
                }
            }
        }
    }
}
