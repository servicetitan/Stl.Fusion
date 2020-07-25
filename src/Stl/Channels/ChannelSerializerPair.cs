using System;
using Stl.Serialization;

namespace Stl.Channels
{
    public class ChannelSerializerPair<T, TSerialized>
    {
        public ITypedSerializer<T, TSerialized> Serializer { get; }
        public ITypedSerializer<T, TSerialized> Deserializer { get; }

        public ChannelSerializerPair(
            ITypedSerializer<T, TSerialized> serializer,
            ITypedSerializer<T, TSerialized> deserializer)
        {
            // This is a safety check: typically serializer & deserializer aren't
            // thread-safe, and even though .WithSerializers doesn't use each one
            // of them concurrently, it won't be true anymore if you pass a single
            // instance here, which might cause way more complex downstream errors.
            if (serializer == deserializer)
                throw new ArgumentOutOfRangeException(nameof(deserializer));
            Serializer = serializer;
            Deserializer = deserializer;
        }

        public void Deconstruct(
            out ITypedSerializer<T, TSerialized> serializer,
            out ITypedSerializer<T, TSerialized> deserializer)
        {
            serializer = Serializer;
            deserializer = Deserializer;
        }

        public ChannelSerializerPair<T, TSerialized> Swap()
            => new ChannelSerializerPair<T, TSerialized>(Deserializer, Serializer);
    }
}
