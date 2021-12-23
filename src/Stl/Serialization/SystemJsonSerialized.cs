namespace Stl.Serialization;

public static class SystemJsonSerialized
{
    public static SystemJsonSerialized<TValue> New<TValue>() => new();
    public static SystemJsonSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static SystemJsonSerialized<TValue> New<TValue>(string data) => new(data);
}

[DataContract]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public class SystemJsonSerialized<T> : TextSerialized<T>
{
    [ThreadStatic] private static ITextSerializer<T>? _serializer;

    public SystemJsonSerialized() { }
    public SystemJsonSerialized(string data) : base(data) { }

    protected override ITextSerializer<T> GetSerializer()
        => _serializer ??= SystemJsonSerializer.Default.ToTyped<T>();
}
