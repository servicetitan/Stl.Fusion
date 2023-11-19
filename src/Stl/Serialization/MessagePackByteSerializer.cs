using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using MessagePack;
using Stl.Internal;
using Stl.Serialization.Internal;
using Errors = Stl.Serialization.Internal.Errors;

namespace Stl.Serialization;

public class MessagePackByteSerializer(MessagePackSerializerOptions options) : IByteSerializer
{
    private readonly ConcurrentDictionary<Type, MessagePackByteSerializer> _typedSerializers = new();

    public static IFormatterResolver DefaultResolver { get; set; } = DefaultMessagePackResolver.Instance;
    public static MessagePackSerializerOptions DefaultOptions { get; set; } = new(DefaultResolver);
    public static MessagePackByteSerializer Default { get; set; } = new(DefaultOptions);

    public MessagePackSerializerOptions Options { get; } = options;

    public MessagePackByteSerializer() : this(DefaultOptions) { }

    public IByteSerializer<T> ToTyped<T>(Type? serializedType = null)
        => (IByteSerializer<T>) GetTypedSerializer(serializedType ?? typeof(T));

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public virtual object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        var serializer = _typedSerializers.GetOrAdd(type,
            static (type1, self) => (MessagePackByteSerializer)typeof(MessagePackByteSerializer<>)
                .MakeGenericType(type1)
                .CreateInstance(self.Options, type1),
            this);
        return serializer.Read(data, type, out readLength);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public virtual void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    {
        var serializer = _typedSerializers.GetOrAdd(type,
            static (type1, self) => (MessagePackByteSerializer)typeof(MessagePackByteSerializer<>)
                .MakeGenericType(type1)
                .CreateInstance(self.Options, type1),
            this);
        serializer.Write(bufferWriter, value, type);
    }

    // Private methods

    private MessagePackByteSerializer GetTypedSerializer(Type serializedType)
        => _typedSerializers.GetOrAdd(serializedType,
            static (type1, self) => (MessagePackByteSerializer)typeof(MessagePackByteSerializer<>)
                .MakeGenericType(type1)
                .CreateInstance(self.Options, type1),
            this);
}

public class MessagePackByteSerializer<T>(MessagePackSerializerOptions options, Type serializedType)
    : MessagePackByteSerializer(options), IByteSerializer<T>
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
        => MessagePackSerializer.Deserialize<T>(data, Options, out readLength);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => MessagePackSerializer.Serialize(bufferWriter, value, Options);
}
