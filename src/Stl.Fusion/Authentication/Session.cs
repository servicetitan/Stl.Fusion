using System.ComponentModel;
using System.Text.Json.Serialization;
using Stl.Conversion;
using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

[DataContract]
[JsonConverter(typeof(SessionJsonConverter))]
[Newtonsoft.Json.JsonConverter(typeof(SessionNewtonsoftJsonConverter))]
[TypeConverter(typeof(SessionTypeConverter))]
public sealed class Session : IHasId<Symbol>, IEquatable<Session>,
    IConvertibleTo<string>, IConvertibleTo<Symbol>
{
    public static Session Null { get; } = null!; // To gracefully bypass some nullability checks

    [DataMember(Order = 0)]
    public Symbol Id { get; }

    public Session(Symbol id)
    {
        // The check is here to prevent use of sessions with empty or other special Ids,
        // which could be a source of security problems later.
        if (id.Value.Length < 8)
            throw Errors.InvalidSessionId(id);
        Id = id;
    }

    // Conversion

    public override string ToString() => Id.Value;

    public static implicit operator Symbol(Session session) => session.Id;
    public static implicit operator string(Session session) => session.Id.Value;

    Symbol IConvertibleTo<Symbol>.Convert() => Id;
    string IConvertibleTo<string>.Convert() => Id.Value;

    // Equality

    public bool Equals(Session? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (ReferenceEquals(null, other))
            return false;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => obj is Session s && Equals(s);
    public override int GetHashCode() => Id.HashCode;
    public static bool operator ==(Session? left, Session? right) => Equals(left, right);
    public static bool operator !=(Session? left, Session? right) => !Equals(left, right);
}
