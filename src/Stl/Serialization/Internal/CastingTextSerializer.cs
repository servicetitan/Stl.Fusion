using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization.Internal;

public class CastingTextSerializer<T>(ITextSerializer untypedSerializer, Type serializedType)
    : ITextSerializer<T>
{
    public ITextSerializer UntypedSerializer { get; } = untypedSerializer;
    public Type SerializedType { get; } = serializedType;
    public bool PreferStringApi => UntypedSerializer.PreferStringApi;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(string data)
        => (T) UntypedSerializer.Read(data, SerializedType)!;
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
        => (T) UntypedSerializer.Read(data, SerializedType, out readLength)!;
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<char> data)
        => (T) UntypedSerializer.Read(data, SerializedType)!;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public string Write(T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => UntypedSerializer.Write(value, SerializedType);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => UntypedSerializer.Write(bufferWriter, value, SerializedType);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(TextWriter textWriter, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => UntypedSerializer.Write(textWriter, value, SerializedType);
}
