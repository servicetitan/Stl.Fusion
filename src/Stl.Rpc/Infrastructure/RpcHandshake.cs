namespace Stl.Rpc.Infrastructure;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RpcHandshake(
    [property: DataMember(Order = 0), MemoryPackOrder(0)] Guid RemotePeerId
    );
