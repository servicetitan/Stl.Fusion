using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization;

public static class NewtonsoftJsonSerialized
{
    public static NewtonsoftJsonSerialized<TValue> New<TValue>() => new();
    public static NewtonsoftJsonSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public static NewtonsoftJsonSerialized<TValue> New<TValue>(string data) => new(data);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public partial class NewtonsoftJsonSerialized<T> : TextSerialized<T>
{
    [ThreadStatic] private static ITextSerializer<T>? _serializer;

    public NewtonsoftJsonSerialized() { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MemoryPackConstructor]
    public NewtonsoftJsonSerialized(string data)
        : base(data) { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    protected override ITextSerializer<T> GetSerializer()
        => _serializer ??= new NewtonsoftJsonSerializer().ToTyped<T>();
}
