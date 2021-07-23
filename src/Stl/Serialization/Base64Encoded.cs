using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Stl.Serialization.Internal;

namespace Stl.Serialization
{
    [Serializable]
    [JsonConverter(typeof(Base64EncodedJsonConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(Base64EncodedNewtonsoftJsonConverter))]
    [TypeConverter(typeof(Base64EncodedTypeConverter))]
    public readonly struct Base64Encoded : IEquatable<Base64Encoded>, IReadOnlyCollection<byte>
    {
        public readonly byte[] Data;

        // Convenience shortcuts
        public int Count => Data.Length;
        public byte this[int index] {
            get => Data[index];
            set => Data[index] = value;
        }

        public Base64Encoded(byte[] data) => Data = data;
        public override string ToString() => $"{GetType()}({Count} byte(s))";

        // IEnumerable
        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();
        public IEnumerator<byte> GetEnumerator() => (Data as IEnumerable<byte>).GetEnumerator();

        // Conversion
        public string? Encode()
            => Data == null! ? null : Convert.ToBase64String(Data);
        public static Base64Encoded Decode(string? encodedData)
            => encodedData == null ? new Base64Encoded(null!) : Convert.FromBase64String(encodedData);

        // Operators
        public static implicit operator Base64Encoded(byte[] data) => new Base64Encoded(data);
        public static implicit operator Base64Encoded(string encodedData) => Decode(encodedData);

        // Equality
        public bool Equals(Base64Encoded other) => Data == other.Data;
        public override bool Equals(object? obj) => obj is Base64Encoded other && Equals(other);
        public override int GetHashCode() => Data.GetHashCode();
        public static bool operator ==(Base64Encoded left, Base64Encoded right) => left.Equals(right);
        public static bool operator !=(Base64Encoded left, Base64Encoded right) => !left.Equals(right);
    }
}
