using System.Buffers;
using MessagePack;
using Stl.Serialization.Internal;

namespace Stl.Serialization;

public class MessagePackByteSerializer : IByteSerializer
{
    private readonly ConcurrentDictionary<Type, MessagePackByteSerializer> _typedSerializers = new();

    public static MessagePackSerializerOptions DefaultOptions { get; set; } = MessagePackSerializer.DefaultOptions;
    public static MessagePackByteSerializer Default { get; } = new(DefaultOptions);

    public MessagePackSerializerOptions Options { get; }

    public MessagePackByteSerializer(MessagePackSerializerOptions? options = null)
        => Options = options ?? DefaultOptions;

    public IByteSerializer<T> ToTyped<T>(Type? serializedType = null)
        => (IByteSerializer<T>) GetTypedSerializer(serializedType ?? typeof(T));

    public virtual object? Read(ReadOnlyMemory<byte> data, Type type)
    {
        var serializer = _typedSerializers.GetOrAdd(type,
            type1 => (MessagePackByteSerializer) typeof(MessagePackByteSerializer<>)
                .MakeGenericType(type1)
                .CreateInstance(Options));
        return serializer.Read(data, type);
    }

    public virtual void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    {
        var serializer = _typedSerializers.GetOrAdd(type,
            type1 => (MessagePackByteSerializer) typeof(MessagePackByteSerializer<>)
                .MakeGenericType(type1)
                .CreateInstance(Options));
        serializer.Write(bufferWriter, value, type);
    }

    // Private methods

    private MessagePackByteSerializer GetTypedSerializer(Type serializedType)
        => _typedSerializers.GetOrAdd(serializedType,
            (type1, self) => (MessagePackByteSerializer) typeof(MessagePackByteSerializer<>)
                .MakeGenericType(type1)
                .CreateInstance(self.Options, type1),
            this);
}

public class MessagePackByteSerializer<T> : MessagePackByteSerializer, IByteSerializer<T>
{
    public Type SerializedType { get; }

    public MessagePackByteSerializer(MessagePackSerializerOptions options, Type serializedType)
        : base(options)
        => SerializedType = serializedType;

    public override object? Read(ReadOnlyMemory<byte> data, Type type)
    {
        if (type != SerializedType)
            throw Errors.SerializedTypeMismatch(SerializedType, type);
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        return Read(data);
    }

    public override void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    {
        if (type != SerializedType)
            throw Errors.SerializedTypeMismatch(SerializedType, type);
        Write(bufferWriter, (T?) value);
    }

    public T Read(ReadOnlyMemory<byte> data)
        => MessagePackSerializer.Deserialize<T>(data, Options);

    public void Write(IBufferWriter<byte> bufferWriter, T? value)
        => MessagePackSerializer.Serialize(bufferWriter, value, Options);
}
