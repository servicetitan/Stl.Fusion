using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization;

public static class MessagePackSerialized
{
    public static MessagePackSerialized<TValue> New<TValue>() => new();
    public static MessagePackSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public static MessagePackSerialized<TValue> New<TValue>(byte[] data) => new(data);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public partial class MessagePackSerialized<T> : ByteSerialized<T>
{
    [ThreadStatic] private static IByteSerializer<T>? _serializer;

    public MessagePackSerialized() { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MemoryPackConstructor]
    public MessagePackSerialized(byte[] data) : base(data) { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    protected override IByteSerializer<T> GetSerializer()
        => _serializer ??= MessagePackByteSerializer.Default.ToTyped<T>();
}
