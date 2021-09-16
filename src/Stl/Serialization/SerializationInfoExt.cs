using System.Runtime.Serialization;

namespace Stl.Serialization
{
    public static class SerializationInfoExt
    {
        public static T? GetValue<T>(this SerializationInfo info, string name)
            => (T?) info.GetValue(name, typeof(T));
    }
}
