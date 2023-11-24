using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization;

public static class TypeDecoratingMemoryPackSerialized
{
    public static TypeDecoratingMemoryPackSerialized<TValue> New<TValue>() => new();
    public static TypeDecoratingMemoryPackSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public static TypeDecoratingMemoryPackSerialized<TValue> New<TValue>(byte[] data) => new(data);
}

#if !NET5_0
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public partial class TypeDecoratingMemoryPackSerialized<T> : ByteSerialized<T>
{
    [ThreadStatic] private static IByteSerializer<T>? _serializer;

    public TypeDecoratingMemoryPackSerialized() { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MemoryPackConstructor]
    public TypeDecoratingMemoryPackSerialized(byte[] data)
        : base(data) { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    protected override IByteSerializer<T> GetSerializer()
        => _serializer ??= new TypeDecoratingByteSerializer(MemoryPackByteSerializer.Default).ToTyped<T>();
}
