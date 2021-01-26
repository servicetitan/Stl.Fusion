using System;
using Stl.Fusion.EntityFramework.Operations;

namespace Stl.Fusion.EntityFramework.Internal
{
    public static class Errors
    {
        public static Exception DbContextIsReadOnly()
            => new InvalidOperationException("This DbContext is read-only.");
        public static Exception OperationScopeIsAlreadyClosed()
            => new InvalidOperationException("Operation scope is already closed (committed or rolled back).");
        public static Exception OperationCommitFailed()
            => new DbOperationFailedException("Couldn't commit the operation.");
    }
}
