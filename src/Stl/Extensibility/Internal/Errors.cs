using System;

namespace Stl.Extensibility.Internal
{
    public static class Errors
    {
        public static Exception CannotConfigureModulesOnceTheyAreCreated()
            => new InvalidOperationException("Cannot configure Modules once they're created.");
    }
}
