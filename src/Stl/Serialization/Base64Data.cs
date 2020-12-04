using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Stl.Serialization.Internal;

namespace Stl.Serialization
{
    [Serializable]
    [JsonConverter(typeof(Base64DataJsonConverter))]
    [TypeConverter(typeof(Base64DataTypeConverter))]
    public readonly struct Base64Data : IEquatable<Base64Data>, IReadOnlyCollection<byte>
    {
        public readonly byte[] Data;

        // Convenience shortcuts
        public int Count => Data.Length;
        public byte this[int index] {
            get => Data[index];
            set => Data[index] = value;
        }

        public Base64Data(byte[] data) => Data = data;
        public override string ToString() => $"{GetType()}({Count} byte(s))";

        // IEnumerable
        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();
        public IEnumerator<byte> GetEnumerator() => (Data as IEnumerable<byte>).GetEnumerator();

        // Conversion
        public string Encode() => Convert.ToBase64String(Data);
        public static Base64Data Decode(string encodedData) => Convert.FromBase64String(encodedData);

        // Operators
        public static implicit operator Base64Data(byte[] data) => new Base64Data(data);
        public static implicit operator Base64Data(string encodedData) => Decode(encodedData);

        // Equality
        public bool Equals(Base64Data other) => Data == other.Data;
        public override bool Equals(object? obj) => obj is Base64Data other && Equals(other);
        public override int GetHashCode() => Data.GetHashCode();
        public static bool operator ==(Base64Data left, Base64Data right) => left.Equals(right);
        public static bool operator !=(Base64Data left, Base64Data right) => !left.Equals(right);
    }
}
