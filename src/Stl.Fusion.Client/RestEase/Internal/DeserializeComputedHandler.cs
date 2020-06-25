using Stl.Extensibility;

namespace Stl.Fusion.Client.RestEase.Internal
{
    internal class DeserializeComputedHandler<T> : 
        HandlerProvider<DeserializeMethodArgs, object?>.IHandler<T>
    {
        public object? Handle(object target, DeserializeMethodArgs arg)
        {
            var rrd = (ReplicaResponseDeserializer) target;
            return rrd.DeserializeComputed<T>(arg.Content, arg.Response, arg.Info);
        }
    }
}
