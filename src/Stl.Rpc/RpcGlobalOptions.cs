using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.Interception;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public record RpcGlobalOptions
{
    public Dictionary<Type, (Symbol, Type?)> ServiceTypes { get; init; } = new();
    public List<Type> MiddlewareTypes { get; init; } = new();

    public Func<Type, Symbol> ServiceNameBuilder { get; init; } = DefaultServiceNameBuilder;
    public Func<RpcMethodDef, Symbol> MethodNameBuilder { get; init; } = DefaultMethodNameBuilder;
    public Func<ArgumentList, Type, object?> ArgumentListSerializer { get; init; } = DefaultArgumentListSerializer;
    public Func<object?, Type, ArgumentList> ArgumentListDeserializer { get; init; } = DefaultArgumentListDeserializer;

    public static Symbol DefaultServiceNameBuilder(Type serviceType)
        => serviceType.GetName();

    public static Symbol DefaultMethodNameBuilder(RpcMethodDef methodDef)
        => $"{methodDef.Method.Name}:{methodDef.RemoteParameterTypes.Length}";

    public static object? DefaultArgumentListSerializer(ArgumentList value, Type type)
    {
        if (value.Length == 0)
            return null;

        var bufferWriter = new ArrayPoolBufferWriter<byte>(256); // We intentionally do not dispose it here
        ByteSerializer.Default.Write(bufferWriter, value, type);
        return bufferWriter;
    }

    public static ArgumentList DefaultArgumentListDeserializer(object? data, Type type)
    {
        if (data is null)
            return ArgumentList.Empty;

        if (data is ReadOnlyMemory<byte> bytes) {
            var result = (ArgumentList?)ByteSerializer.Default.Read(bytes, type);
            return result ?? ArgumentList.Empty;
        }
        if (data is ArrayPoolBufferWriter<byte> bufferWriter) {
            var result = (ArgumentList?)ByteSerializer.Default.Read(bufferWriter.WrittenMemory, type);
            bufferWriter.Dispose();
            return result ?? ArgumentList.Empty;
        }

        throw new ArgumentOutOfRangeException(nameof(data));
    }
}
