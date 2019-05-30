using System;

namespace Stl.Reactionist.Internal
{
    public static class Errors
    {
        public static Exception DependencyTrackerIsActive() =>
            new InvalidOperationException("This method cannot be used when dependency tracker is active.");
    }
}
