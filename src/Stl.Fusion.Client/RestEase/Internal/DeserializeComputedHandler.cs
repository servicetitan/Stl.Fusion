using Stl.Extensibility;

namespace Stl.Fusion.Client.RestEase.Internal
{
    internal class DeserializeComputedHandler<T> : 
        HandlerProvider<DeserializeMethodArgs, object?>.IHandler<T>
    {
        public bool IsWrongHandler = typeof(T) == typeof(DeserializeComputedHandlerProvider); 

        public object? Handle(object target, DeserializeMethodArgs arg)
        {
            if (IsWrongHandler)
                return null;
            var rrd = (ReplicaResponseDeserializer) target;
            return rrd.DeserializeComputed<T>(arg.Content, arg.Response, arg.Info);
        }
    }
}
