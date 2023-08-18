namespace Stl.Serialization;

public static class TypeDecoratingMemoryPackSerialized
{
    public static TypeDecoratingMemoryPackSerialized<TValue> New<TValue>() => new();
    public static TypeDecoratingMemoryPackSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static TypeDecoratingMemoryPackSerialized<TValue> New<TValue>(byte[] data) => new(data);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public partial class TypeDecoratingMemoryPackSerialized<T> : ByteSerialized<T>
{
    [ThreadStatic] private static IByteSerializer<T>? _serializer;

    public TypeDecoratingMemoryPackSerialized() { }

    [MemoryPackConstructor]
    public TypeDecoratingMemoryPackSerialized(byte[] data)
        : base(data) { }

    protected override IByteSerializer<T> GetSerializer()
        => _serializer ??= new TypeDecoratingByteSerializer(MemoryPackByteSerializer.Default).ToTyped<T>();
}
