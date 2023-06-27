using System.Buffers;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.Interception;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public sealed class RpcByteArgumentSerializer : RpcArgumentSerializer
{
    private readonly IByteSerializer _serializer;

    public RpcByteArgumentSerializer(IByteSerializer serializer)
        => _serializer = serializer;

    public override TextOrBytes Serialize(ArgumentList arguments, bool allowPolymorphism)
    {
        if (arguments.Length == 0)
            return TextOrBytes.EmptyBytes;

        using var buffer = new ArrayPoolBufferWriter<byte>(256);
        var serializer = allowPolymorphism
            ? (ItemSerializer)new ItemPolymorphicSerializer(_serializer, buffer)
            : new ItemNonPolymorphicSerializer(_serializer, buffer);
        arguments.Read(serializer);
        return new TextOrBytes(buffer.WrittenSpan.ToArray());
    }

    public override void Deserialize(ref ArgumentList arguments, bool allowPolymorphism, TextOrBytes data)
    {
        if (!data.IsBytes(out var bytes))
            throw new ArgumentOutOfRangeException(nameof(data));
        if (bytes.IsEmpty)
            return;

        var deserializer = allowPolymorphism
            ? (ItemDeserializer)new ItemPolymorphicDeserializer(_serializer, bytes)
            : new ItemNonPolymorphicDeserializer(_serializer, bytes);
        arguments.Write(deserializer);
    }

    // Nested types

    private abstract class ItemSerializer : ArgumentListReader
    {
        protected readonly IByteSerializer Serializer;
        protected readonly IBufferWriter<byte> Buffer;

        protected ItemSerializer(IByteSerializer serializer, IBufferWriter<byte> buffer)
        {
            Serializer = serializer;
            Buffer = buffer;
        }

        public override void OnStruct<T>(T item, int index)
        {
            if (typeof(T) != typeof(CancellationToken))
                Serializer.Write(Buffer, item);
        }
    }

    private class ItemPolymorphicSerializer : ItemSerializer
    {
        public ItemPolymorphicSerializer(IByteSerializer serializer, IBufferWriter<byte> buffer)
            : base(serializer, buffer) { }

        public override void OnObject(Type type, object? item, int index)
        {
            var itemType = item?.GetType() ?? type;
            var typeRef = itemType == type ? default : new TypeRef(itemType).WithoutAssemblyVersions();
            Serializer.Write(Buffer, typeRef);
            Serializer.Write(Buffer, item, itemType);
        }
    }

    private class ItemNonPolymorphicSerializer : ItemSerializer
    {
        public ItemNonPolymorphicSerializer(IByteSerializer serializer, IBufferWriter<byte> buffer)
            : base(serializer, buffer) { }

        public override void OnObject(Type type, object? item, int index)
        {
            Serializer.Write(Buffer, default(TypeRef));
            Serializer.Write(Buffer, item, type);
        }
    }

    private abstract class ItemDeserializer : ArgumentListWriter
    {
        protected readonly IByteSerializer Serializer;
        protected ReadOnlyMemory<byte> Data;

        protected ItemDeserializer(IByteSerializer serializer, ReadOnlyMemory<byte> data)
        {
            Serializer = serializer;
            Data = data;
        }

        public override T OnStruct<T>(int index)
            => typeof(T) == typeof(CancellationToken)
                ? default!
                : Serializer.Read<T>(ref Data);
    }

    private class ItemPolymorphicDeserializer : ItemDeserializer
    {
        public ItemPolymorphicDeserializer(IByteSerializer serializer, ReadOnlyMemory<byte> data)
            : base(serializer, data) { }

        public override object? OnObject(Type type, int index)
        {
            var typeRef = Serializer.Read<TypeRef>(ref Data);
            var itemType = typeRef == default ? type : typeRef.Resolve();
            if (itemType != type && !type.IsAssignableFrom(itemType))
                throw Errors.CannotDeserializeUnexpectedPolymorphicArgumentType(type, itemType);

            return Serializer.Read(ref Data, itemType);
        }
    }

    private class ItemNonPolymorphicDeserializer : ItemDeserializer
    {
        public ItemNonPolymorphicDeserializer(IByteSerializer serializer, ReadOnlyMemory<byte> data)
            : base(serializer, data) { }

        public override object? OnObject(Type type, int index)
        {
            var typeRef = Serializer.Read<TypeRef>(ref Data);
            var itemType = typeRef == default ? type : typeRef.Resolve();
            if (itemType != type)
                throw Errors.CannotDeserializeUnexpectedArgumentType(type, itemType);

            return Serializer.Read(ref Data, type);
        }
    }
}
