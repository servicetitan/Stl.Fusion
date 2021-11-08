using System.Text.Json.Serialization;

namespace Stl.Fusion.Bridge;

[DataContract]
[StructLayout(LayoutKind.Sequential)]
public readonly struct PublicationRef : IEquatable<PublicationRef>
{
    [DataMember(Order = 0)]
    public Symbol PublisherId { get; }
    [DataMember(Order = 1)]
    public Symbol PublicationId { get; }

    [JsonConstructor]
    public PublicationRef(Symbol publisherId, Symbol publicationId)
    {
        PublisherId = publisherId;
        PublicationId = publicationId;
    }

    // Conversion

    public override string ToString() => $"{PublisherId.Value}/{PublicationId.Value}";

    public void Deconstruct(out Symbol publisherId, out Symbol publicationId)
    {
        publisherId = PublisherId;
        publicationId = PublicationId;
    }

    public static implicit operator PublicationRef((Symbol, Symbol) pair)
        => new PublicationRef(pair.Item1, pair.Item2);

    // Equality

    public bool Equals(PublicationRef other)
        => PublisherId.Equals(other.PublisherId) && PublicationId.Equals(other.PublicationId);
    public override bool Equals(object? obj)
        => obj is PublicationRef other && Equals(other);
    public override int GetHashCode()
        => HashCode.Combine(PublisherId, PublicationId);
    public static bool operator ==(PublicationRef left, PublicationRef right) => left.Equals(right);
    public static bool operator !=(PublicationRef left, PublicationRef right) => !left.Equals(right);
}
