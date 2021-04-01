using System;
using System.Collections.Concurrent;
using Stl.Reflection;

namespace Stl.Fusion.Blazor
{
    public abstract class ParameterComparer
    {
        private static readonly ConcurrentDictionary<Type, ParameterComparer> Cache = new();
        public static ParameterComparer Default { get; } = new DefaultParameterComparer();

        public abstract bool AreEqual(object? oldValue, object? newValue);

        public static ParameterComparer Get(Type comparerType)
            => Cache.GetOrAdd(comparerType, comparerType1 => {
                if (!typeof(ParameterComparer).IsAssignableFrom(comparerType1))
                    throw new ArgumentOutOfRangeException(nameof(comparerType));
                return (ParameterComparer) comparerType1.CreateInstance();
            });
    }

    public class DefaultParameterComparer : ParameterComparer
    {
        // Mostly copied from Microsoft.AspNetCore.Components.ChangeDetection
        public override bool AreEqual(object? oldValue, object? newValue)
        {
            var oldIsNotNull = oldValue != null;
            var newIsNotNull = newValue != null;
            if (oldIsNotNull != newIsNotNull)
                return false; // One's null and the other isn't, so different
            if (oldIsNotNull) {
                // Both are not null (considering previous check)
                var oldValueType = oldValue!.GetType();
                var newValueType = newValue!.GetType();
                if (oldValueType != newValueType            // Definitely different
                    || !IsKnownImmutableType(oldValueType)  // Maybe different
                    || !oldValue.Equals(newValue))          // Somebody says they are different
                    return false;
            }

            // By now we know either both are null, or they are the same immutable type
            // and ThatType::Equals says the two values are equal.
            return true;
        }

        // The contents of this list need to trade off false negatives against computation
        // time. So we don't want a huge list of types to check (or would have to move to
        // a hashtable lookup, which is differently expensive). It's better not to include
        // uncommon types here even if they are known to be immutable.
        private static bool IsKnownImmutableType(Type type)
            => type.IsPrimitive
                || type == typeof(string)
                || type == typeof(DateTime)
                || type == typeof(Type)
                || type == typeof(decimal);
    }

    public class ByValueParameterComparer : ParameterComparer
    {
        public override bool AreEqual(object? oldValue, object? newValue)
            => Equals(oldValue, newValue);
    }

    public class ByReferenceParameterComparer : ParameterComparer
    {
        public override bool AreEqual(object? oldValue, object? newValue)
            => ReferenceEquals(oldValue, newValue);
    }

    public class AlwaysEqualParameterComparer : ParameterComparer
    {
        public override bool AreEqual(object? oldValue, object? newValue)
            => true;
    }
}
