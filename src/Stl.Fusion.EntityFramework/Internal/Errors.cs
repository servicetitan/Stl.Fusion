using System;
using System.Collections.Generic;
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

        public static Exception AuthenticatedUserRequired()
            => new InvalidOperationException("This method requires an authenticated user.");
        public static Exception ForcedSignOut()
            => Stl.Fusion.Authentication.Internal.Errors.ForcedSignOut();
        public static Exception NoOperationsFrameworkServices()
            => new InvalidOperationException(
                "Operations Framework services aren't registered. " +
                "Call DbContextBuilder<TDbContext>.AddDbOperations before calling this method to add them.");

        public static Exception EntityNotFound<TEntity>()
            => EntityNotFound(typeof(TEntity));
        public static Exception EntityNotFound(Type entityType)
            => new KeyNotFoundException($"Requested {entityType.Name} entity is not found.");
    }
}
