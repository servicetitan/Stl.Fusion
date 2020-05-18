using System;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class StateChangeMessage : ReplicaMessage
    {
        public bool HasOutput { get; set; }
        public LTag NewLTag { get; set; }
        public bool NewIsConsistent { get; set; }

        public abstract Type GetResultType();
    }

    [Serializable]
    public class StateChangeMessage<T> : StateChangeMessage
    {
        public Result<T> Output { get; set; }

        public override Type GetResultType() => typeof(T);
    }
}
