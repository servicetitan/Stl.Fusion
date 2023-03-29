namespace Stl.Fusion.Bridge;

[DataContract]
[StructLayout(LayoutKind.Auto)]
public readonly record struct PublicationRef(
    [property: DataMember(Order = 0)] Symbol PublisherId,
    [property: DataMember(Order = 0)] Symbol PublicationId)
{
    public static PublicationRef None { get; } = default;

    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public bool IsNone => PublicationId.IsEmpty;

    public Replica? Resolve()
        => ReplicaRegistry.Instance.Get(this);

    // Conversion

    public override string ToString() => $"{PublisherId.Value}/{PublicationId.Value}";

    public static implicit operator PublicationRef((Symbol, Symbol) pair)
        => new(pair.Item1, pair.Item2);
}
