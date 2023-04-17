namespace Stl.Rpc;

public record RpcChannelOptions
{
    public static IReadOnlyList<Type> DefaultMiddlewares { get; set; } = ImmutableList<Type>.Empty;
#if NET6_0_OR_GREATER
    public static IReadOnlySet<Type> DefaultServices { get; set; } = 
#else
    public static HashSet<Type> DefaultServices { get; set; } =
#endif
        new HashSet<Type>();

    public IReadOnlyList<Type> Middlewares { get; init; } = DefaultMiddlewares;
#if NET6_0_OR_GREATER
    public IReadOnlySet<Type> Services { get; init; } = DefaultServices;
#else
    public HashSet<Type> Services { get; init; } = DefaultServices;
#endif
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
