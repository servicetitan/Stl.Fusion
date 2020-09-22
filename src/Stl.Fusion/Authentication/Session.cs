using System;
using System.ComponentModel;
using System.Threading;
using Newtonsoft.Json;
using Stl.Extensibility;
using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication
{
    [Serializable]
    [JsonConverter(typeof(SessionJsonConverter))]
    [TypeConverter(typeof(SessionTypeConverter))]
    public class Session : IHasId<string>, IEquatable<Session>, IConvertibleTo<string>
    {
        internal static readonly AsyncLocal<Session?> CurrentLocal = new AsyncLocal<Session?>();

        public static Session? Current => CurrentLocal.Value;
        public string Id { get; }

        [JsonConstructor]
        public Session(string id) => Id = id;

        public override string ToString() => Id;
        string IConvertibleTo<string>.Convert() => Id;

        // Equality

        public virtual bool Equals(Session? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(null, other))
                return false;
            if (other.GetType() != GetType())
                return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => obj is Session s && Equals(s);
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(Session? left, Session? right) => Equals(left, right);
        public static bool operator !=(Session? left, Session? right) => !Equals(left, right);
    }
}
