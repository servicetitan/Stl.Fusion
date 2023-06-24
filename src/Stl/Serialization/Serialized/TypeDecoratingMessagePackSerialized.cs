namespace Stl.Serialization;

public static class TypeDecoratingMessagePackSerialized
{
    public static TypeDecoratingMemoryPackSerialized<TValue> New<TValue>() => new();
    public static TypeDecoratingMemoryPackSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static TypeDecoratingMemoryPackSerialized<TValue> New<TValue>(byte[] data) => new(data);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public partial class TypeDecoratingMessagePackSerialized<T> : ByteSerialized<T>
{
    [ThreadStatic] private static IByteSerializer<T>? _serializer;

    public TypeDecoratingMessagePackSerialized() { }

    [MemoryPackConstructor]
    public TypeDecoratingMessagePackSerialized(byte[] data)
        : base(data) { }

    protected override IByteSerializer<T> GetSerializer()
        => _serializer ??= new TypeDecoratingByteSerializer(MessagePackByteSerializer.Default).ToTyped<T>();
}
