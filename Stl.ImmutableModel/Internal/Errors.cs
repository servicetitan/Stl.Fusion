using System;

namespace Stl.ImmutableModel.Internal
{
    public static class Errors
    {
        public static Exception InvalidUpdateKeyMismatch() =>
            new ArgumentException("Invalid update: source.Key != target.Key.");

        public static Exception MoreThanOneTypeMapsToTheSameLocalKey(
            Type type, Type otherType, in Symbol localKey) =>
            new ArgumentException($"More than one type maps to the same local key: " +
                $"'{type.FullName}', '{otherType.FullName}' -> {localKey}");
    }
}
