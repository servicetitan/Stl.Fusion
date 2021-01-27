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

        public static Exception CannotCreateUnauthenticatedUser(string paramName)
            => new ArgumentOutOfRangeException(paramName, "Can't create unauthenticated user.");
        public static Exception CannotUseForcedSignOutSession()
            => new InvalidOperationException("Can't use Session once a sign-out was forced there.");

        public static Exception NoOperationsFrameworkServices()
            => new InvalidOperationException(
                "Operations Framework services aren't registered. " +
                "Call DbContextBuilder<TDbContext>.AddDbOperations before calling this method to add them.");
    }
}
