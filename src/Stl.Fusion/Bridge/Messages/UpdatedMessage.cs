using System;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class UpdatedMessage : PublicationMessage
    {
        public int Tag { get; set; }

        public abstract Type GetResultType();
    }

    [Serializable]
    public class UpdatedMessage<T> : UpdatedMessage
    {
        public Result<T> Output { get; set; }

        public override Type GetResultType() => typeof(T);
    }
}
