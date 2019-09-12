using System.Runtime.Serialization;

namespace Stl.Serialization
{
    public interface INotifyDeserialized
    {
        void OnDeserialized(StreamingContext context);
    }
}
