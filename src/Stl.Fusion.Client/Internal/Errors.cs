using System;

namespace Stl.Fusion.Client.Internal
{
    public static class Errors
    {
        public static Exception InvalidUri(string uri)
            => new InvalidOperationException($"Invalid URI: '{uri}'.");

        public static Exception ComputedOfTExpected(string argumentName)
            => new ArgumentOutOfRangeException(argumentName,
                "Only typeof(IComputed<T>) values are supported for this argument.");
    }
}
