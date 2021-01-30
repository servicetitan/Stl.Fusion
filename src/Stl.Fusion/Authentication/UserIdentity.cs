using System;
using Newtonsoft.Json;
using Stl.Text;

namespace Stl.Fusion.Authentication
{
    public readonly struct UserIdentity : IEquatable<UserIdentity>
    {
        private static readonly ListFormat IdFormat = ListFormat.SlashSeparated;
        public static UserIdentity None { get; } = default;
        public static string DefaultSchema { get; } = "Default";

        public Symbol Id { get; }
        [JsonIgnore]
        public string Schema => ParseId(Id).Schema;
        [JsonIgnore]
        public string SchemaBoundId => ParseId(Id).SchemaBoundId;
        [JsonIgnore]
        public bool IsValid => !Id.IsEmpty;

        [JsonConstructor]
        public UserIdentity(Symbol id)
            => Id = id;
        public UserIdentity(string id)
            => Id = id;
        public UserIdentity(string provider, string providerBoundId)
            => Id = FormatId(provider, providerBoundId);

        // Conversion

        public override string ToString() => $"{GetType().Name}({Id})";

        public void Deconstruct(out string schema, out string schemaBoundId)
            => (schema, schemaBoundId) = ParseId(Id);

        public static implicit operator UserIdentity((string Schema, string SchemaBoundId) source)
            => new(source.Schema, source.SchemaBoundId);
        public static implicit operator UserIdentity(Symbol source) => new(source);
        public static implicit operator UserIdentity(string source) => new(source);
        public static implicit operator Symbol(UserIdentity source) => source.Id;
        public static implicit operator string(UserIdentity source) => source.Id.Value;

        // Equality

        public bool Equals(UserIdentity other) => Id.Equals(other.Id);
        public override bool Equals(object? obj) => obj is UserIdentity other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(UserIdentity left, UserIdentity right) => left.Equals(right);
        public static bool operator !=(UserIdentity left, UserIdentity right) => !left.Equals(right);

        // Private methods

        private static string FormatId(string schema, string schemaBoundId)
        {
            var formatter = IdFormat.CreateFormatter(StringBuilderEx.Acquire());
            try {
                if (schema != DefaultSchema)
                    formatter.Append(schema);
                formatter.Append(schemaBoundId);
                formatter.AppendEnd();
                return formatter.Output;
            }
            finally {
                formatter.OutputBuilder.Release();
            }
        }

        private static (string Schema, string SchemaBoundId) ParseId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return ("", "");
            var parser = IdFormat.CreateParser(id, StringBuilderEx.Acquire());
            try {
                if (!parser.TryParseNext())
                    return (DefaultSchema, id);
                var firstItem = parser.Item;
                return parser.TryParseNext() ? (firstItem, parser.Item) : (DefaultSchema, firstItem);
            }
            finally {
                parser.ItemBuilder.Release();
            }
        }
    }
}
