using System.Diagnostics;
using Stl.Interception;
using Stl.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

#pragma warning disable MA0055

[DataContract]
public abstract partial class RpcStream : IRpcObject
{
    protected static readonly ConcurrentDictionary<object, Unit> ActiveObjects = new();
    protected static readonly UnboundedChannelOptions RemoteChannelOptions = new() {
        SingleReader = true,
        SingleWriter = true,
        AllowSynchronousContinuations = false, // We don't want sync handlers to "clog" the call processing loop
    };

    [DataMember, MemoryPackOrder(1)]
    public int AckPeriod { get; init; } = 30;
    [DataMember, MemoryPackOrder(2)]
    public int AckAdvance { get; init; } = 61;

    // Non-serialized members
    [JsonIgnore, MemoryPackIgnore] public RpcObjectId Id { get; protected set; }
    [JsonIgnore, MemoryPackIgnore] public RpcPeer? Peer { get; protected set; }
    [JsonIgnore, MemoryPackIgnore] public abstract Type ItemType { get; }
    [JsonIgnore, MemoryPackIgnore] public abstract RpcObjectKind Kind { get; }

    public static RpcStream<T> New<T>(IAsyncEnumerable<T> outgoingSource)
        => new(outgoingSource);
    public static RpcStream<T> New<T>(IEnumerable<T> outgoingSource)
        => new(outgoingSource.ToAsyncEnumerable());

    public override string ToString()
        => $"{GetType().GetName()}(#{Id} @ {Peer?.Ref}, {Kind})";

    Task IRpcObject.Reconnect(CancellationToken cancellationToken)
        => Reconnect(cancellationToken);

    void IRpcObject.Disconnect()
        => Disconnect();

    // Protected methods

    protected internal abstract ArgumentList CreateStreamItemArguments();
    protected internal abstract ArgumentList CreateStreamBatchArguments();
    protected internal abstract Task OnItem(long index, object? item);
    protected internal abstract Task OnBatch(long index, object? items);
    protected internal abstract Task OnEnd(long index, Exception? error);
    protected abstract Task Reconnect(CancellationToken cancellationToken);
    protected abstract void Disconnect();
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class RpcStream<T> : RpcStream, IAsyncEnumerable<T>
{
    private readonly IAsyncEnumerable<T>? _localSource;
    private Channel<T>? _remoteChannel;
    private long _nextIndex;
    private bool _isRegistered;
    private bool _isDisconnected;
    private readonly object _lock;

    [DataMember, MemoryPackOrder(0)]
    public RpcObjectId SerializedId {
        get {
            // This member must be never accessed directly - its only purpose is to be called on serialization
            this.RequireKind(RpcObjectKind.Local);
            if (!Id.IsNone) // Already registered
                return Id;

            Peer ??= RpcOutboundContext.Current?.Peer ?? RpcInboundContext.GetCurrent().Peer;
            var sharedObjects = Peer.SharedObjects;
            Id = sharedObjects.NextId(); // NOTE: Id changes on serialization!
            var sharedStream = new RpcSharedStream<T>(this);
            sharedObjects.Register(sharedStream);
            return Id;
        }
        set {
            this.RequireKind(RpcObjectKind.Remote);
            lock (_lock) {
                if (!Id.IsNone) {
                    if (Id == value)
                        return;
                    throw Errors.AlreadyInitialized(nameof(SerializedId));
                }

                Id = value;
                Peer = RpcInboundContext.GetCurrent().Peer;
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
            Close(Errors.AlreadyDisposed(GetType()));
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        if (_localSource != null)
            return _localSource.GetAsyncEnumerator(cancellationToken);

        lock (_lock) {
            if (_remoteChannel != null)
                throw Internal.Errors.RemoteRpcStreamCanBeEnumeratedJustOnce();
            if (Peer == null)
                throw Errors.InternalError("RpcStream.Peer == null.");

            _remoteChannel = Channel.CreateUnbounded<T>(RemoteChannelOptions);
            if (_nextIndex == long.MaxValue) // Marked as missing
                _remoteChannel.Writer.TryComplete(Internal.Errors.RpcStreamNotFound());
            else {
                _isRegistered = true;
                Peer.RemoteObjects.Register(this);
                _ = SendResetFromLock(0);
            }
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
        => ArgumentList.New<long, T>(0L, default!);

    protected internal override ArgumentList CreateStreamBatchArguments()
        => ArgumentList.New<long, T[]>(0L, default!);

    protected internal override Task OnItem(long index, object? item)
    {
        lock (_lock) {
            if (_remoteChannel == null)
                return Task.CompletedTask;
            if (index < _nextIndex)
                return MaybeSendAckFromLock(index);
            if (index > _nextIndex)
                return SendResetFromLock(_nextIndex);

            // Debug.WriteLine($"{Id}: +#{index} (ack @ {ackIndex})");
            _nextIndex++;
            _remoteChannel.Writer.TryWrite((T)item!); // Must always succeed for unbounded channel
            return Task.CompletedTask;
        }
    }

    protected internal override Task OnBatch(long index, object? items)
    {
        lock (_lock) {
            if (_remoteChannel == null)
                return Task.CompletedTask;
            if (index < _nextIndex)
                return MaybeSendAckFromLock(index);
            if (index > _nextIndex)
                return SendResetFromLock(_nextIndex);

            var typedItems = (T[])items!;
            foreach (var item in typedItems) {
                // Debug.WriteLine($"{Id}: +#{index} (ack @ {ackIndex})");
                _nextIndex++;
                _remoteChannel.Writer.TryWrite(item); // Must always succeed for unbounded channel
            }
            return Task.CompletedTask;
        }
    }

    protected internal override Task OnEnd(long index, Exception? error)
    {
        lock (_lock) {
            if (_remoteChannel == null)
                return Task.CompletedTask;
            if (index < _nextIndex)
                return MaybeSendAckFromLock(index);
            if (index > _nextIndex)
                return SendResetFromLock(_nextIndex);

            // Debug.WriteLine($"{Id}: +{index} (ended!)");
            CloseFromLock(error);
            return Task.CompletedTask;
        }
    }

    protected override Task Reconnect(CancellationToken cancellationToken)
    {
        lock (_lock)
            return _remoteChannel != null && !_isDisconnected
                ? SendResetFromLock(_nextIndex)
                : Task.CompletedTask;
    }

    protected override void Disconnect()
    {
        lock (_lock) {
            if (_isDisconnected)
                return;

            _isDisconnected = true;
            CloseFromLock(Internal.Errors.RpcStreamNotFound());
        }
    }

    // Private methods

    private void Close(Exception? error)
    {
        lock (_lock)
            CloseFromLock(error);
    }

    private void CloseFromLock(Exception? error)
    {
        if (_remoteChannel != null) {
            if (_nextIndex != long.MaxValue)
                _ = SendCloseFromLock();
            _remoteChannel.Writer.TryComplete(error);
        }
        if (_isRegistered) {
            _isRegistered = false;
            Peer?.RemoteObjects.Unregister(this);
        }
    }

    private Task SendCloseFromLock()
    {
        _nextIndex = int.MaxValue;
        return SendAckFromLock(_nextIndex, true);
    }

    private Task SendResetFromLock(long index)
        => SendAckFromLock(index, true);

    private Task MaybeSendAckFromLock(long index)
        => index % AckPeriod == 0 && index > 0
            ? SendAckFromLock(index)
            : Task.CompletedTask;

    private Task SendAckFromLock(long index, bool mustReset = false)
    {
        // Debug.WriteLine($"{Id}: <- ACK: ({index}, {mustReset})");
        return !_isDisconnected
            ? Peer!.Hub.SystemCallSender.Ack(Peer, Id.LocalId, index, mustReset ? Id.HostId : default)
            : Task.CompletedTask;
    }

    // Nested types

    private class RemoteChannelEnumerator : IAsyncEnumerator<T>
    {
        private readonly ChannelReader<T> _reader;
        private long _nextIndex;
        private bool _isEnded;
        private Result<T> _current;
        private readonly RpcStream<T> _stream;
        private readonly CancellationToken _cancellationToken;

        public RemoteChannelEnumerator(RpcStream<T> stream, CancellationToken cancellationToken)
        {
            _stream = stream;
            _cancellationToken = cancellationToken;
            _reader = stream._remoteChannel!.Reader;
            ActiveObjects.TryAdd(this, default);
        }

        public T Current => _nextIndex != 0
            ? _current.Value
            : throw new InvalidOperationException($"{nameof(MoveNextAsync)} should be called first.");

        public ValueTask DisposeAsync()
        {
            if (!ActiveObjects.TryRemove(this, out _))
                return default;

            _stream.Close(Errors.AlreadyDisposed(GetType()));
            return default;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            if (_isEnded)
                return default;

            var ackTask = _stream.MaybeSendAckFromLock(_nextIndex);
            try {
                if (_reader.TryRead(out var current))
                    _current = current;
                else
                    return MoveNext(ackTask);
            }
            catch (Exception e) {
                _current = Result.Error<T>(e);
                _isEnded = true;
            }

            _nextIndex++;
            return ackTask.IsCompleted
                ? ValueTaskExt.TrueTask
                : new ValueTask<bool>(ackTask.ContinueWith(_ => true, TaskScheduler.Default));

            async ValueTask<bool> MoveNext(Task taskToAwait) {
                if (!taskToAwait.IsCompleted)
                    await taskToAwait.SilentAwait(false);
                try {
                    if (!await _reader.WaitToReadAsync(_cancellationToken).ConfigureAwait(false)) {
                        _isEnded = true;
                        return false;
                    }
                    if (!_reader.TryRead(out var current1))
                        throw Errors.InternalError("Couldn't read after successful WaitToReadAsync call.");

                    await _stream.MaybeSendAckFromLock(_nextIndex).ConfigureAwait(false);
                    _current = current1;
                    _nextIndex++;
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
