using System.Text.Json.Serialization;

namespace Stl.Fusion.Authentication;

[DataContract]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public readonly struct UserIdentity : IEquatable<UserIdentity>
{
    private static readonly ListFormat IdFormat = ListFormat.SlashSeparated;
    public static UserIdentity None { get; } = default;
    public static string DefaultSchema { get; } = "Default";

    [DataMember(Order = 0)]
    public Symbol Id { get; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public string Schema => ParseId(Id.Value).Schema;
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public string SchemaBoundId => ParseId(Id.Value).SchemaBoundId;
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public bool IsValid => !Id.IsEmpty;

    [JsonConstructor, Newtonsoft.Json.JsonConstructor]
    public UserIdentity(Symbol id)
        => Id = id;
    public UserIdentity(string id)
        => Id = id;
    public UserIdentity(string provider, string providerBoundId)
        => Id = FormatId(provider, providerBoundId);

    // Conversion

    // NOTE: ToString() has to return Id.Value, otherwise dictionary key
    // serialization will be broken, and UserIdentity is actually used
    // as a dictionary key in User.Identities
    public override string ToString() => Id.Value;

    public void Deconstruct(out string schema, out string schemaBoundId)
        => (schema, schemaBoundId) = ParseId(Id.Value);

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
        using var f = IdFormat.CreateFormatter();
        if (schema != DefaultSchema)
            f.Append(schema);
        f.Append(schemaBoundId);
        f.AppendEnd();
        return f.Output;
    }

    private static (string Schema, string SchemaBoundId) ParseId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return ("", "");
        using var p = IdFormat.CreateParser(id);
        if (!p.TryParseNext())
            return (DefaultSchema, id);
        var firstItem = p.Item;
        return p.TryParseNext() ? (firstItem, p.Item) : (DefaultSchema, firstItem);
    }
}
