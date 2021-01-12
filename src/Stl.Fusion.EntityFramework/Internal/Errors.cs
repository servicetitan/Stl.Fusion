using System;

namespace Stl.Fusion.EntityFramework.Internal
{
    public static class Errors
    {
        public static Exception DbContextIsReadOnly()
            => new InvalidOperationException("This DbContext is read-only.");
        public static Exception TransactionScopeIsAlreadyClosed()
            => new InvalidOperationException("The transaction scope is already closed (committed or rolled back).");
    }
}
