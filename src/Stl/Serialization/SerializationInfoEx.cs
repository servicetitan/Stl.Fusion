using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Stl.Serialization
{
    public static class SerializationInfoEx
    {
        [return: MaybeNull]
        public static T GetValue<T>(this SerializationInfo info, string name)
            => (T) info.GetValue(name, typeof(T))!;
    }
}
