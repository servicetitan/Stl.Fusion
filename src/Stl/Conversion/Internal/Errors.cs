using System;

namespace Stl.Conversion.Internal
{
    public static class Errors
    {
        public static Exception NoConverter(Type sourceType, Type targetType)
            => new NotSupportedException($"There is no '{sourceType}' -> '{targetType} converter.");

        public static Exception CantConvert(Type sourceType, Type targetType)
            => new InvalidOperationException($"Provided '{sourceType}' instance is not convertable to '{targetType}.");
    }
}
