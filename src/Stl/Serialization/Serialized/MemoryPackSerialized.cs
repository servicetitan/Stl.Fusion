namespace Stl.Serialization;

public static class MemoryPackSerialized
{
    public static MemoryPackSerialized<TValue> New<TValue>() => new();
    public static MemoryPackSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static MemoryPackSerialized<TValue> New<TValue>(byte[] data) => new(data);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public partial class MemoryPackSerialized<T> : ByteSerialized<T>
{
    [ThreadStatic] private static IByteSerializer<T>? _serializer;

    public MemoryPackSerialized() { }

    [MemoryPackConstructor]
    public MemoryPackSerialized(byte[] data) : base(data) { }

    protected override IByteSerializer<T> GetSerializer()
        => _serializer ??= MemoryPackByteSerializer.Default.ToTyped<T>();
}
