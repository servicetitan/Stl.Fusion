using System;

namespace Stl.Serialization
{
    public class UntypedToTypedUtf16Serializer<T> : IUtf16Serializer<T>, IUtf16Reader<T>, IUtf16Writer<T>
    {
        public IUtf16Serializer Serializer { get; }
        public Type SerializedType { get; }
        public IUtf16Reader<T> Reader => this;
        public IUtf16Writer<T> Writer => this;

        public UntypedToTypedUtf16Serializer(IUtf16Serializer serializer, Type serializedType)
        {
            Serializer = serializer;
            SerializedType = serializedType;
        }

        public T Read(string data)
            => (T) Serializer.Reader.Read(data, SerializedType)!;

        public string Write(T value)
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            => Serializer.Writer.Write(value, SerializedType);
    }
}
