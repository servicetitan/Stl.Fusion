namespace Stl.Rpc;

public record RpcChannelOptions
{
    public Func<Type, bool> MiddlewareFilter { get; init; } = static _ => true;
    public Func<Type, bool> ServiceFilter { get; init; } = static _ => true;
    public Func<object?, Type, object?> Deserializer { get; init; } = DefaultDeserializer;

    public static Func<RpcChannel, RpcChannelOptions> DefaultOptionsProvider(IServiceProvider services)
        => _ => new RpcChannelOptions();

    public static object? DefaultDeserializer(object? data, Type type)
    {
        if (data == null)
            return null;

        if (data is ReadOnlyMemory<byte> bytes)
            return ByteSerializer.Default.Read(bytes, type);
        if (data is string text)
            return TextSerializer.Default.Read(text, type);

        throw new ArgumentOutOfRangeException(nameof(data));
    }
}
