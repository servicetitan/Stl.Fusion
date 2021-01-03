using System;

namespace Stl.EntityFramework.Internal
{
    public static class Errors
    {
        public static Exception DbContextIsReadOnly()
            => new InvalidOperationException("This DbContext is read-only.");
    }
}
