using System;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class PublicationStateChangedMessage : PublicationMessage
    {
        public LTag Version { get; set; }
        public bool IsConsistent { get; set; }
        public abstract Type GetResultType();
    }

    [Serializable]
    public class PublicationStateChangedMessage<T> : PublicationStateChangedMessage
    {
        public Result<T> Output { get; set; } = default;
        public override Type GetResultType() => typeof(T);
    }
}
