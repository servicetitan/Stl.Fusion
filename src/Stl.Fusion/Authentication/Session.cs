using System.ComponentModel;
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
    public static Session Default { get; } = new("~");

    private string? _hash;

    [DataMember(Order = 0)]
    public Symbol Id { get; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public string Hash => _hash ??= Id.Value.GetMD5HashCode();

    public Session(Symbol id)
    {
        // The check is here to prevent use of sessions with empty or other special Ids,
        // which could be a source of security problems later.
        var idValue = id.Value;
        if (idValue.Length < 8 && !(idValue.Length == 1 && idValue[0] == '~'))
            throw Errors.InvalidSessionId(id);
        Id = id;
    }

    public Symbol GetTenantId()
    {
        var idValue = Id.Value;
        var atIndex = idValue.IndexOf('@');
        if (atIndex <= 0)
            return Symbol.Empty;
        return idValue[(atIndex + 1)..];
    }

    public Session WithTenantId(Symbol tenantId)
    {
        var idValue = Id.Value;
        var atIndex = idValue.IndexOf('@');
        if (atIndex < 0)
            return tenantId.IsEmpty ? this : new Session($"{idValue}@{tenantId.Value}");
        var prefix = idValue[..atIndex];
        return new Session(tenantId.IsEmpty ? prefix :$"{prefix}@{tenantId.Value}");
    }

    // Conversion

    public override string ToString() => Id.Value;

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
