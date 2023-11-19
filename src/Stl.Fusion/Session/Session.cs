using System.ComponentModel;
using System.Globalization;
using Microsoft.Toolkit.HighPerformance;
using Stl.Conversion;
using Stl.Fusion.Internal;

namespace Stl.Fusion;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[JsonConverter(typeof(SessionJsonConverter))]
[Newtonsoft.Json.JsonConverter(typeof(SessionNewtonsoftJsonConverter))]
[TypeConverter(typeof(SessionTypeConverter))]
public sealed partial class Session : IHasId<Symbol>, IRequirementTarget,
    IEquatable<Session>, IConvertibleTo<string>, IConvertibleTo<Symbol>,
    IHasJsonCompatibleToString
{
    public static readonly Session Default = new("~");
    public static SessionFactory Factory { get; set; } = DefaultSessionFactory.New();
    public static SessionValidator Validator { get; set; } = session => !session.IsDefault();

    private string? _hash;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public Symbol Id { get; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember, MemoryPackIgnore]
    public string Hash => _hash ??= ComputeHash();

    public static Session New()
        => Factory.Invoke();

    [MemoryPackConstructor]
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
        var atIndex = idValue.IndexOf('@', StringComparison.Ordinal);
        if (atIndex <= 0)
            return Symbol.Empty;
        return idValue[(atIndex + 1)..];
    }

    public Session WithTenantId(Symbol tenantId)
    {
        var idValue = Id.Value;
        var atIndex = idValue.IndexOf('@', StringComparison.Ordinal);
        if (atIndex < 0)
            return tenantId.IsEmpty ? this : new Session($"{idValue}@{tenantId.Value}");
        var prefix = idValue[..atIndex];
        return new Session(tenantId.IsEmpty ? prefix :$"{prefix}@{tenantId.Value}");
    }

    // We use non-cryptographic hash here because System.Security.Cryptography isn't supported in Blazor.
    // The length of hash is much smaller than Session.Id, so it's still almost impossible to guess
    // SessionId by knowing it; on the other hand, ~4B hash variants are enough to identify
    // a Session of a given user, and that's the only purpose of this hash.
    private string ComputeHash()
        => ((uint) Id.Value.GetDjb2HashCode()).ToString("x8", CultureInfo.InvariantCulture);

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
