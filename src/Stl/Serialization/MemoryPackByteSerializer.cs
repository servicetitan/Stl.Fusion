using System.Buffers;
using Stl.Serialization.Internal;

#if NETSTANDARD2_0
using MessagePack;
#endif

namespace Stl.Serialization;

public class MemoryPackByteSerializer(MemoryPackSerializerOptions options) : IByteSerializer
{
    private readonly ConcurrentDictionary<Type, MemoryPackByteSerializer> _typedSerializers = new();

    public static readonly MemoryPackSerializerOptions DefaultOptions = MemoryPackSerializerOptions.Default;
    public static readonly MemoryPackByteSerializer Default = new(DefaultOptions);

    public MemoryPackSerializerOptions Options { get; } = options;

    public MemoryPackByteSerializer() : this(DefaultOptions) { }

    public IByteSerializer<T> ToTyped<T>(Type? serializedType = null)
        => (IByteSerializer<T>) GetTypedSerializer(serializedType ?? typeof(T));

    public virtual object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        var serializer = _typedSerializers.GetOrAdd(type,
            static (type1, self) => (MemoryPackByteSerializer)typeof(MemoryPackByteSerializer<>)
                .MakeGenericType(type1)
                .CreateInstance(self.Options, type1),
            this);
        return serializer.Read(data, type, out readLength);
    }

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

public class MemoryPackByteSerializer<T> : MemoryPackByteSerializer, IByteSerializer<T>
{
    public Type SerializedType { get; }

    public MemoryPackByteSerializer(MemoryPackSerializerOptions options, Type serializedType)
        : base(options)
        => SerializedType = serializedType;

    public override object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        if (type != SerializedType)
            throw Errors.SerializedTypeMismatch(SerializedType, type);

        // ReSharper disable once HeapView.PossibleBoxingAllocation
        return Read(data, out readLength);
    }

    public override void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    {
        if (type != SerializedType)
            throw Errors.SerializedTypeMismatch(SerializedType, type);

        Write(bufferWriter, (T)value!);
    }

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

    public void Write(IBufferWriter<byte> bufferWriter, T value)
#if !NETSTANDARD2_0
        => MemoryPackSerializer.Serialize(bufferWriter, value, Options);
#else
        => MessagePackSerializer.Serialize(bufferWriter, value, MessagePackByteSerializer.DefaultOptions);
#endif
}
