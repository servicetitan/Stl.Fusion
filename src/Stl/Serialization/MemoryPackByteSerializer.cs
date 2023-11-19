using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;
using Errors = Stl.Serialization.Internal.Errors;

#if NETSTANDARD2_0
using MessagePack;
#endif

namespace Stl.Serialization;

public class MemoryPackByteSerializer(MemoryPackSerializerOptions options) : IByteSerializer
{
    private readonly ConcurrentDictionary<Type, MemoryPackByteSerializer> _typedSerializers = new();

    public static MemoryPackSerializerOptions DefaultOptions { get; set; } = MemoryPackSerializerOptions.Default;
    public static MemoryPackByteSerializer Default { get; set; } = new(DefaultOptions);

    public MemoryPackSerializerOptions Options { get; } = options;

    public MemoryPackByteSerializer() : this(DefaultOptions) { }

    public IByteSerializer<T> ToTyped<T>(Type? serializedType = null)
        => (IByteSerializer<T>) GetTypedSerializer(serializedType ?? typeof(T));

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public virtual object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        var serializer = _typedSerializers.GetOrAdd(type,
            static (type1, self) => (MemoryPackByteSerializer)typeof(MemoryPackByteSerializer<>)
                .MakeGenericType(type1)
                .CreateInstance(self.Options, type1),
            this);
        return serializer.Read(data, type, out readLength);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public virtual void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    {
        var serializer = _typedSerializers.GetOrAdd(type,
            static (type1, self) => (MemoryPackByteSerializer)typeof(MemoryPackByteSerializer<>)
                .MakeGenericType(type1)
                .CreateInstance(self.Options, type1),
            this);
        serializer.Write(bufferWriter, value, type);
    }

    // Private methods

    private MemoryPackByteSerializer GetTypedSerializer(Type serializedType)
        => _typedSerializers.GetOrAdd(serializedType,
            static (type1, self) => (MemoryPackByteSerializer)typeof(MemoryPackByteSerializer<>)
                .MakeGenericType(type1)
                .CreateInstance(self.Options, type1),
            this);
}

public class MemoryPackByteSerializer<T>(MemoryPackSerializerOptions options, Type serializedType)
    : MemoryPackByteSerializer(options), IByteSerializer<T>
{
    public Type SerializedType { get; } = serializedType;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        if (type != SerializedType)
            throw Errors.SerializedTypeMismatch(SerializedType, type);

        // ReSharper disable once HeapView.PossibleBoxingAllocation
        return Read(data, out readLength);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    {
        if (type != SerializedType)
            throw Errors.SerializedTypeMismatch(SerializedType, type);

        Write(bufferWriter, (T)value!);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
    {
#if !NETSTANDARD2_0
        var result = default(T);
        readLength = MemoryPackSerializer.Deserialize(data.Span, ref result, Options);
        return result!;
#else
        return MessagePackSerializer.Deserialize<T>(data, MessagePackByteSerializer.DefaultOptions, out readLength);
#endif
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, T value)
#if !NETSTANDARD2_0
        => MemoryPackSerializer.Serialize(bufferWriter, value, Options);
#else
        => MessagePackSerializer.Serialize(bufferWriter, value, MessagePackByteSerializer.DefaultOptions);
#endif
}
