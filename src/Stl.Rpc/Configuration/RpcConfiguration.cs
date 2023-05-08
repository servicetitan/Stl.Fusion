using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.Interception;

namespace Stl.Rpc;

public record RpcConfiguration
{
    public Dictionary<Type, RpcServiceConfiguration> Services { get; init; } = new();

    public Func<Type, Symbol> ServiceNameBuilder { get; init; } = DefaultServiceNameBuilder;
    public Func<RpcMethodDef, Symbol> MethodNameBuilder { get; init; } = DefaultMethodNameBuilder;
    public Func<ArgumentList, Type, object?> ArgumentSerializer { get; init; } = DefaultArgumentSerializer;
    public Func<object?, Type, ArgumentList> ArgumentDeserializer { get; init; } = DefaultArgumentDeserializer;

    public static Symbol DefaultServiceNameBuilder(Type serviceType)
        => serviceType.GetName();

    public static Symbol DefaultMethodNameBuilder(RpcMethodDef methodDef)
        => $"{methodDef.Method.Name}:{methodDef.RemoteParameterTypes.Length}";

    public static object? DefaultArgumentSerializer(ArgumentList value, Type type)
    {
        if (value.Length == 0)
            return null;

        var bufferWriter = new ArrayPoolBufferWriter<byte>(256); // We intentionally do not dispose it here
        ByteSerializer.Default.Write(bufferWriter, value, type);
        return bufferWriter;
    }

    public static ArgumentList DefaultArgumentDeserializer(object? data, Type type)
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
