using System;
using System.ComponentModel;
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
        public string Id { get; }

        [JsonConstructor]
        public Session(string id)
        {
            // The check is here to prevent use of sessions with empty or other special Ids,
            // which could be a source of security problems later.
            if (id.Length < 8)
                throw Errors.InvalidSessionId(id);
            Id = id;
        }

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
