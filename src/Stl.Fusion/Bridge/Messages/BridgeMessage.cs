namespace Stl.Fusion.Bridge.Messages;

[DataContract]
public abstract class BridgeMessage
{
    public override string ToString()
        => $"{GetType().Name} {JsonFormatter.Format(this)}";
}
