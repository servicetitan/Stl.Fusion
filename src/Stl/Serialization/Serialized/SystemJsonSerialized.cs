using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization;

public static class SystemJsonSerialized
{
    public static SystemJsonSerialized<TValue> New<TValue>() => new();
    public static SystemJsonSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public static SystemJsonSerialized<TValue> New<TValue>(string data) => new(data);
}

#if !NET5_0
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public partial class SystemJsonSerialized<T> : TextSerialized<T>
{
    [ThreadStatic] private static ITextSerializer<T>? _serializer;

    public SystemJsonSerialized() { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MemoryPackConstructor]
    public SystemJsonSerialized(string data) : base(data) { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    protected override ITextSerializer<T> GetSerializer()
        => _serializer ??= SystemJsonSerializer.Default.ToTyped<T>();
}
