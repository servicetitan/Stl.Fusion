using System;
using System.Reflection;
using Stl.Frozen;

namespace Stl.Fusion.Blazor
{
    public static class CloneEx
    {
        public static T Clone<T>(this T source)
        {
            switch (source) {
            case IFrozen f:
                return (T) f.CloneToUnfrozen(true);
            default:
                var memberwiseCloneMethod = typeof(object).GetMethod(
                    nameof(MemberwiseClone),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                return (T) memberwiseCloneMethod!.Invoke(source, Array.Empty<object>());
            }
        }

        public static T Clone<T>(this T source, Action<T>? updater)
        {
            var clone = source.Clone();
            updater?.Invoke(clone);
            return clone;
        }
    }
}
