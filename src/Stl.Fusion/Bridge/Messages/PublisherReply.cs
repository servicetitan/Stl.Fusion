namespace Stl.Fusion.Bridge.Messages;

[DataContract]
public abstract class PublisherReply : BridgeMessage
{
    [DataMember(Order = 0)]
    public Symbol PublisherId { get; set; }
    [DataMember(Order = 1)]
    public long? MessageIndex { get; set; }
}
