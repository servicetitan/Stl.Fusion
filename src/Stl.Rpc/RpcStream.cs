using Stl.Interception;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public enum RpcStreamKind
{
    Incoming = 0,
    Outgoing,
}

[DataContract]
public abstract partial class RpcStream
{
    public static RpcStream<T> New<T>(IAsyncEnumerable<T> outgoingSource)
        => new(outgoingSource);

    [JsonInclude, Newtonsoft.Json.JsonProperty, DataMember, MemoryPackOrder(0)]
    protected long SerializedId {
        get {
            // This member must be never accessed directly - its only purpose is to be called on serialization
            AssertKind(RpcStreamKind.Outgoing);
            Kind = RpcStreamKind.Outgoing;
            Peer = RpcOutboundContext.GetCurrent().Peer!;
            Id = Peer.OutgoingStreams.Register(this); // NOTE: Id may change on serialization!
            return Id;
        }
        set {
            AssertKind(RpcStreamKind.Incoming);
            Id = value;
            Peer = RpcInboundContext.GetCurrent().Peer;
            Peer.IncomingStreams.Register(this);
        }
    }

    [JsonInclude, Newtonsoft.Json.JsonProperty, DataMember, MemoryPackOrder(0)]
    public int WindowSize { get; init; }

    // Non-serialized members
    [JsonIgnore, MemoryPackIgnore] public long Id { get; private set; }
    [JsonIgnore, MemoryPackIgnore] public RpcPeer? Peer { get; private set; }
    [JsonIgnore, MemoryPackIgnore] public abstract Type ItemType { get; }
    [JsonIgnore, MemoryPackIgnore] public RpcStreamKind Kind { get; private set; } = RpcStreamKind.Outgoing;

    protected RpcStream(RpcStreamKind kind)
        => Kind = kind;

    public override string ToString()
        => $"{GetType().GetName()}(#{Id} @ {Peer?.Ref}, {Kind})";

    protected void AssertKind(RpcStreamKind expectedKind)
    {
        if (Kind != expectedKind)
            throw Errors.InvalidRpcStreamKind(expectedKind);
    }

    protected internal abstract ArgumentList CreateStreamItemArguments();
    protected internal abstract void OnItem(object? item);
    protected internal abstract void OnEnd(ExceptionInfo? error);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class RpcStream<T> : RpcStream
{
    private readonly IAsyncEnumerable<T>? _outgoingSource;

    [JsonIgnore] public override Type ItemType => typeof(T);

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public RpcStream() : base(RpcStreamKind.Incoming) { }
    public RpcStream(IAsyncEnumerable<T> outgoingSource) : base(RpcStreamKind.Outgoing)
        => _outgoingSource = outgoingSource;

    protected internal override ArgumentList CreateStreamItemArguments()
        => ArgumentList.New(default(T));

    protected internal override void OnItem(object? item)
    {
        throw new NotImplementedException();
    }

    protected internal override void OnEnd(ExceptionInfo? error)
    {
        throw new NotImplementedException();
    }
}
