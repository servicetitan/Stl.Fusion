using System;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class PublicationStateChangedMessage : ReplicaMessage
    {
        public bool HasOutput { get; set; }
        public LTag NewLTag { get; set; }
        public bool NewIsConsistent { get; set; }

        public abstract Type GetResultType();
    }

    [Serializable]
    public class PublicationStateChangedMessage<T> : PublicationStateChangedMessage
    {
        public Result<T> Output { get; set; }

        public override Type GetResultType() => typeof(T);
    }
}
