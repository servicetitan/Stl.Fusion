namespace Stl.Serialization;

public static class TypeDecoratingSystemJsonSerialized
{
    public static TypeDecoratingSystemJsonSerialized<TValue> New<TValue>() => new();
    public static TypeDecoratingSystemJsonSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static TypeDecoratingSystemJsonSerialized<TValue> New<TValue>(string data) => new(data);
}

[DataContract]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public class TypeDecoratingSystemJsonSerialized<T> : TextSerialized<T>
{
    [ThreadStatic] private static ITextSerializer<T>? _serializer;

    public TypeDecoratingSystemJsonSerialized() { }
    public TypeDecoratingSystemJsonSerialized(string data) : base(data) { }

    protected override ITextSerializer<T> GetSerializer()
        => _serializer ??= new TypeDecoratingSerializer(SystemJsonSerializer.Default).ToTyped<T>();
}
