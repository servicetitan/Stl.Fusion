namespace Stl.Fusion.Bridge.Messages;

[DataContract]
public class WelcomeReply : PublisherReply
{
    [DataMember(Order = 2)]
    public bool IsAccepted { get; set; }
}
