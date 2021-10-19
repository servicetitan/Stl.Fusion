namespace Stl.Fusion.Bridge.Messages;

[DataContract]
public abstract class ReplicatorRequest : BridgeMessage
{
    [DataMember(Order = 0)]
    public Symbol ReplicatorId { get; set; }
}
