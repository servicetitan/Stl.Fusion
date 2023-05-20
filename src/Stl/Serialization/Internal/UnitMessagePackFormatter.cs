using MessagePack;
using MessagePack.Formatters;

namespace Stl.Serialization.Internal;

public class UnitMessagePackFormatter : IMessagePackFormatter<Unit>, IFormatterResolver
{
    public static readonly UnitMessagePackFormatter Instance = new();
    public static IFormatterResolver Resolver => Instance;

    private UnitMessagePackFormatter() { }

    public IMessagePackFormatter<T>? GetFormatter<T>()
        => typeof(T) == typeof(Unit)
            ? (IMessagePackFormatter<T>)Instance
            : null;

    public void Serialize(ref MessagePackWriter writer, Unit value, MessagePackSerializerOptions options) 
        => writer.WriteNil();

    public Unit Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        reader.ReadNil();
        return default;
    }
}
