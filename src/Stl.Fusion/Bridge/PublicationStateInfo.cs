namespace Stl.Fusion.Bridge;

[DataContract]
public class PublicationStateInfo
{
    [DataMember(Order = 0)]
    public PublicationRef PublicationRef { get; set; }
    [DataMember(Order = 1)]
    public LTag Version { get; set; }
    [DataMember(Order = 2)]
    public bool IsConsistent { get; set; }

    public PublicationStateInfo() { }
    public PublicationStateInfo(PublicationRef publicationRef)
        => PublicationRef = publicationRef;
    public PublicationStateInfo(PublicationRef publicationRef, LTag version, bool isConsistent)
    {
        PublicationRef = publicationRef;
        Version = version;
        IsConsistent = isConsistent;
    }
}

[DataContract]
public class PublicationStateInfo<T> : PublicationStateInfo
{
    [DataMember(Order = 3)]
    public Result<T> Output { get; set; }

    public PublicationStateInfo() { }
    public PublicationStateInfo(PublicationRef publicationRef) : base(publicationRef) { }
    public PublicationStateInfo(PublicationStateInfo stateInfo, Result<T> output = default)
        : this(stateInfo.PublicationRef, stateInfo.Version, stateInfo.IsConsistent, output) { }
    public PublicationStateInfo(PublicationRef publicationRef, LTag version, bool isConsistent, Result<T> output = default)
        : base(publicationRef, version, isConsistent)
        => Output = output;
}
