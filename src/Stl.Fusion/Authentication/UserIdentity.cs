using System;
using Newtonsoft.Json;
using Stl.Text;

namespace Stl.Fusion.Authentication
{
    public readonly struct UserIdentity : IEquatable<UserIdentity>
    {
        private static readonly ListFormat IdFormat = ListFormat.SlashSeparated;
        public static string DefaultAuthenticationType { get; } = "Default";
        private readonly Symbol _id;

        // Never returns Symbol.Null, though Symbol.Empty is allowed
        public Symbol Id => ReferenceEquals(_id.Value, null!) ? Symbol.Empty : _id;

        [JsonIgnore]
        public bool IsAuthenticated => !string.IsNullOrEmpty(Id.Value);

        [JsonConstructor]
        public UserIdentity(Symbol id)
            => _id = id;
        public UserIdentity(string id)
            => _id = id;
        public UserIdentity(string provider, string providerBoundId)
            => _id = FormatId(provider, providerBoundId);

        // Conversion

        public override string ToString() => $"{GetType().Name}({Id})";

        public void Deconstruct(out string authenticationType, out string userId)
            => (authenticationType, userId) = ParseId(Id);

        public static implicit operator UserIdentity((string AuthenticationType, string UserId) source)
            => new(source.AuthenticationType, source.UserId);
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

        // Static FormatId, ParseId

        public static string FormatId(string authenticationType, string userId)
        {
            var formatter = IdFormat.CreateFormatter(StringBuilderEx.Acquire());
            try {
                if (authenticationType != DefaultAuthenticationType)
                    formatter.Append(authenticationType);
                formatter.Append(userId);
                formatter.AppendEnd();
                return formatter.Output;
            }
            finally {
                formatter.OutputBuilder.Release();
            }
        }

        public static (string AuthenticationType, string UserId) ParseId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return ("", "");
            var parser = IdFormat.CreateParser(id, StringBuilderEx.Acquire());
            try {
                if (!parser.TryParseNext())
                    return (DefaultAuthenticationType, id);
                var firstItem = parser.Item;
                return parser.TryParseNext() ? (firstItem, parser.Item) : (DefaultAuthenticationType, firstItem);
            }
            finally {
                parser.ItemBuilder.Release();
            }
        }
    }
}
