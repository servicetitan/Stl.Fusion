using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Interception;
using Stl.Internal;
using Stl.IO;
using Errors = Stl.Rpc.Internal.Errors;

namespace Stl.Rpc;

public sealed class RpcByteArgumentSerializer(IByteSerializer serializer) : RpcArgumentSerializer
{
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override TextOrBytes Serialize(ArgumentList arguments, bool allowPolymorphism)
    {
        if (arguments.Length == 0)
            return TextOrBytes.EmptyBytes;

        using var buffer = new ArrayPoolBuffer<byte>(256);
        var itemSerializer = allowPolymorphism
            ? (ItemSerializer)new ItemPolymorphicSerializer(serializer, buffer)
            : new ItemNonPolymorphicSerializer(serializer, buffer);
        arguments.Read(itemSerializer);
        return new TextOrBytes(buffer.WrittenSpan.ToArray()); // That's why we retain the last buffer
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override void Deserialize(ref ArgumentList arguments, bool allowPolymorphism, TextOrBytes data)
    {
        if (!data.IsBytes(out var bytes))
            throw new ArgumentOutOfRangeException(nameof(data));
        if (bytes.IsEmpty)
            return;

        var deserializer = allowPolymorphism
            ? (ItemDeserializer)new ItemPolymorphicDeserializer(serializer, bytes)
            : new ItemNonPolymorphicDeserializer(serializer, bytes);
        arguments.Write(deserializer);
    }

    // Nested types

    private abstract class ItemSerializer(IByteSerializer serializer, IBufferWriter<byte> buffer) : ArgumentListReader
    {
        protected readonly IByteSerializer Serializer = serializer;
        protected readonly IBufferWriter<byte> Buffer = buffer;

        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
        public override void OnStruct<T>(T item, int index)
        {
            if (typeof(T) != typeof(CancellationToken))
                Serializer.Write(Buffer, item);
        }
    }

    private sealed class ItemPolymorphicSerializer(IByteSerializer serializer, IBufferWriter<byte> buffer)
        : ItemSerializer(serializer, buffer)
    {
        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
        public override void OnObject(Type type, object? item, int index)
        {
            var itemType = item?.GetType() ?? type;
            var typeRef = itemType == type ? default : new TypeRef(itemType).WithoutAssemblyVersions();
            Serializer.Write(Buffer, typeRef);
            Serializer.Write(Buffer, item, itemType);
        }
    }

    private sealed class ItemNonPolymorphicSerializer(IByteSerializer serializer, IBufferWriter<byte> buffer)
        : ItemSerializer(serializer, buffer)
    {
        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
        public override void OnObject(Type type, object? item, int index)
        {
            Serializer.Write(Buffer, default(TypeRef));
            Serializer.Write(Buffer, item, type);
        }
    }

    private abstract class ItemDeserializer(IByteSerializer serializer, ReadOnlyMemory<byte> data) : ArgumentListWriter
    {
        protected readonly IByteSerializer Serializer = serializer;
        protected ReadOnlyMemory<byte> Data = data;

        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
        public override T OnStruct<T>(int index)
            => typeof(T) == typeof(CancellationToken)
                ? default!
                : Serializer.Read<T>(ref Data);
    }

    private sealed class ItemPolymorphicDeserializer(IByteSerializer serializer, ReadOnlyMemory<byte> data)
        : ItemDeserializer(serializer, data)
    {
        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
        public override object? OnObject(Type type, int index)
        {
            var typeRef = Serializer.Read<TypeRef>(ref Data);
            var itemType = typeRef == default ? type : typeRef.Resolve();
            if (itemType != type && !type.IsAssignableFrom(itemType))
                throw Errors.CannotDeserializeUnexpectedPolymorphicArgumentType(type, itemType);

            return Serializer.Read(ref Data, itemType);
        }
    }

    private sealed class ItemNonPolymorphicDeserializer(IByteSerializer serializer, ReadOnlyMemory<byte> data)
        : ItemDeserializer(serializer, data)
    {
        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
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
