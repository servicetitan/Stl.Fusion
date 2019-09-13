using System;

namespace Stl.ImmutableModel.Internal
{
    public static class Errors
    {
        public static Exception InvalidUpdateKeyMismatch() =>
            new ArgumentException("Invalid update: source.Key != target.Key.");
    }
}
