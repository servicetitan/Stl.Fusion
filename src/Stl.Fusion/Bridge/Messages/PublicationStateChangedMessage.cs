using System;
using Newtonsoft.Json;

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
        public T OutputValue { get; set; } = default!;
        public Type? OutputErrorType { get; set; }
        public string? OutputErrorMessage { get; set; } 

        public override Type GetResultType() => typeof(T);
    }
}
