namespace Stl.Rpc;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[method: MemoryPackConstructor, JsonConstructor, Newtonsoft.Json.JsonConstructor]
public record struct RpcStreamId(
    [property: DataMember(Order = 0), MemoryPackOrder(0)] Symbol Id)
{
    public static readonly RpcStreamId None = new(default);

    [MemoryPackIgnore] public string Value => Id.Value;

    public override string ToString() => Id.ToString();

    // Conversion

    public static implicit operator RpcStreamId(Symbol id) => new(id);
    public static implicit operator RpcStreamId(string id) => new(id);
}
