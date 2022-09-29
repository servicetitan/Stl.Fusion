namespace Stl.Fusion.Bridge.Messages;

[DataContract]
public class SubscribeRequest : ReplicaRequest
{
    [DataMember(Order = 3)]
    public LTag Version { get; set; }
    [DataMember(Order = 4)]
    public bool IsConsistent { get; set; }
}
