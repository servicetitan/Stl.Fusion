using System;
using System.ComponentModel;
using System.Threading;
using Newtonsoft.Json;
using Stl.Extensibility;
using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication
{
    [Serializable]
    [JsonConverter(typeof(AuthContextJsonConverter))]
    [TypeConverter(typeof(AuthContextTypeConverter))]
    public class AuthContext : IHasId<string>, IEquatable<AuthContext>, IConvertibleTo<string>
    {
        internal static readonly AsyncLocal<AuthContext?> CurrentLocal = new AsyncLocal<AuthContext?>();

        public static AuthContext? Current => CurrentLocal.Value;
        public string Id { get; }

        [JsonConstructor]
        public AuthContext(string id) => Id = id;

        public override string ToString() => Id;
        string IConvertibleTo<string>.Convert() => Id;

        // Equality

        public virtual bool Equals(AuthContext? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(null, other))
                return false;
            if (other.GetType() != GetType())
                return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => obj is AuthContext s && Equals(s);
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(AuthContext? left, AuthContext? right) => Equals(left, right);
        public static bool operator !=(AuthContext? left, AuthContext? right) => !Equals(left, right);
    }
}
