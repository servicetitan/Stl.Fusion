using System;

namespace Stl.Fusion.Server.Internal
{
    public static class Errors
    {
        public static Exception AlreadyPublished()
            => new InvalidOperationException(
                "Only one publication can be published for a given HTTP request.");

    }
}
