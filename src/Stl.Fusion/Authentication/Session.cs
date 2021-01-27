using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Stl.Fusion.Authentication.Internal;
using Stl.Text;

namespace Stl.Fusion.Authentication
{
    [Serializable]
    [JsonConverter(typeof(SessionJsonConverter))]
    [TypeConverter(typeof(SessionTypeConverter))]
    public sealed class Session : IHasId<string>, IEquatable<Session>,
        IConvertibleTo<string>, IConvertibleTo<Symbol>
    {
        private readonly Symbol _id;

        public string Id => _id.Value;

        [JsonConstructor]
        public Session(string id)
        {
            // The check is here to prevent use of sessions with empty or other special Ids,
            // which could be a source of security problems later.
            if (id.Length < 8)
                throw Errors.InvalidSessionId(id);
            _id = id;
        }

        public override string ToString() => Id;
        public Symbol ToSymbol() => _id;

        string IConvertibleTo<string>.Convert() => Id;
        Symbol IConvertibleTo<Symbol>.Convert() => _id;

        // Equality

        public bool Equals(Session? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(null, other))
                return false;
            return _id == other._id;
        }

        public override bool Equals(object? obj) => obj is Session s && Equals(s);
        public override int GetHashCode() => _id.GetHashCode();
        public static bool operator ==(Session? left, Session? right) => Equals(left, right);
        public static bool operator !=(Session? left, Session? right) => !Equals(left, right);
    }
}
