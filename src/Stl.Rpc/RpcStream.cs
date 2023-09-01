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

    [JsonInclude, Newtonsoft.Json.JsonProperty, DataMember, MemoryPackOrder(0)]
    protected long SerializedId {
        get {
            // This member must be never accessed directly - its only purpose is to be called on serialization
            this.RequireKind(RpcObjectKind.Local);
            Peer = RpcOutboundContext.GetCurrent().Peer!;
            var localObjects = Peer.LocalObjects;
            Id = localObjects.NextId(); // NOTE: Id changes on serialization!
            var sharedStream = new RpcLocalStream(this);
            localObjects.Register(sharedStream);
            return Id;
        }
        set {
            this.RequireKind(RpcObjectKind.Remote);
            Id = value;
            Peer = RpcInboundContext.GetCurrent().Peer;
            Peer.RemoteObjects.Register(this);
        }
    }

    [JsonInclude, Newtonsoft.Json.JsonProperty, DataMember, MemoryPackOrder(0)]
    public int BlockSize { get; init; }

    // Non-serialized members
    [JsonIgnore, MemoryPackIgnore] public long Id { get; private set; }
    [JsonIgnore, MemoryPackIgnore] public RpcPeer? Peer { get; private set; }
    [JsonIgnore, MemoryPackIgnore] public abstract Type ItemType { get; }
    [JsonIgnore, MemoryPackIgnore] public abstract RpcObjectKind Kind { get; }

    public override string ToString()
        => $"{GetType().GetName()}(#{Id} @ {Peer?.Ref}, {Kind})";

    // Protected methods

    protected internal abstract ArgumentList CreateStreamItemArguments();
    protected internal abstract void OnItem(long index, object? item);
    protected internal abstract void OnEnd(ExceptionInfo? error);
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
            TryCompleteUnsafe(Errors.AlreadyDisposed(GetType()));
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        if (_localSource != null)
            return _localSource.GetAsyncEnumerator(cancellationToken);

        lock (_lock) {
            if (_remoteChannel != null)
                throw Internal.Errors.RemoteRpcStreamCanBeEnumeratedJustOnce();

            _remoteChannel = Channel.CreateUnbounded<T>(RemoteChannelOptions);
            return new Enumerator(this, cancellationToken);
        }
    }

    // Protected methods

    protected internal override ArgumentList CreateStreamItemArguments()
        => ArgumentList.New(0L, default(T));

    protected internal override void OnItem(long index, object? item)
    {
        lock (_lock) {
            if (_remoteChannel == null)
                throw Errors.InternalError("RpcStream got an item before the enumeration.");

            if (index < _nextIndex)
                return;

            if (index > _nextIndex) {
                TryCompleteUnsafe(Errors.InternalError($"RpcStream item #{_nextIndex} is missing."));
                return;
            }

            _nextIndex++;
            if (!_remoteChannel.Writer.TryWrite((T)item!))
                TryCompleteUnsafe(Errors.InternalError("RpcStream failed to synchronously add an item."));
        }
    }

    protected internal override void OnEnd(ExceptionInfo? error)
    {
        lock (_lock) {
            _isCloseAckSent = true;
            TryCompleteUnsafe(error is { } vError ? vError.ToException() : null);
        }
    }

    // Private methods

    private void TryComplete(Exception? error = null)
    {
        lock (_lock)
            TryCompleteUnsafe(error);
    }

    private void TryCompleteUnsafe(Exception? error = null)
    {
        if (_remoteChannel != null) {
            if (!_isCloseAckSent) {
                _isCloseAckSent = true;
                _ = SendCloseAck();
            }
            _remoteChannel.Writer.TryComplete(error);
        }
        if (_localSource == null && !_isUnregistered) {
            _isUnregistered = true;
            Peer?.RemoteObjects.Unregister(this);
        }
    }

    private ValueTask SendCloseAck()
        => SendAck(long.MaxValue);

    private ValueTask SendAck(long index)
    {
        var peer = Peer!;
        return peer.Hub.SystemCallSender.StreamAck(peer, Id, index);
    }

    // Nested types

    private class Enumerator(RpcStream<T> stream, CancellationToken cancellationToken) : IAsyncEnumerator<T>
    {
        private readonly IAsyncEnumerator<T> _baseEnumerator =  stream._remoteChannel!.Reader
            .ReadAllAsync(cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        private bool _isFirst = true;

        public T Current => _baseEnumerator.Current;

        public ValueTask DisposeAsync()
        {
            stream.TryComplete();
            return _baseEnumerator.DisposeAsync();
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return _isFirst ? SendAckAndMoveNext() : _baseEnumerator.MoveNextAsync();

            async ValueTask<bool> SendAckAndMoveNext() {
                _isFirst = false;
                await stream.SendAck(0).ConfigureAwait(false);
                return await _baseEnumerator.MoveNextAsync().ConfigureAwait(false);
            }
        }
    }
}
