using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization;

public static class MemoryPackSerialized
{
    public static MemoryPackSerialized<TValue> New<TValue>() => new();
    public static MemoryPackSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public static MemoryPackSerialized<TValue> New<TValue>(byte[] data) => new(data);
}

#if !NET5_0
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public partial class MemoryPackSerialized<T> : ByteSerialized<T>
{
    [ThreadStatic] private static IByteSerializer<T>? _serializer;

    public MemoryPackSerialized() { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MemoryPackConstructor]
    public MemoryPackSerialized(byte[] data) : base(data) { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    protected override IByteSerializer<T> GetSerializer()
        => _serializer ??= MemoryPackByteSerializer.Default.ToTyped<T>();
}
