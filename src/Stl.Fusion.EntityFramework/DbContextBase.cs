using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace Stl.Fusion.EntityFramework
{
    /// <summary>
    /// This type solves a single problem: currently EF Core 6.0
    /// doesn't properly dispose pooled DbContexts rendering
    /// them unusable after disposal.
    /// Details: https://github.com/dotnet/efcore/issues/26202
    /// </summary>
    public abstract class DbContextBase : DbContext
    {
#if NET6_0
        private static readonly FieldInfo LeaseField =
            typeof(DbContext).GetField("_lease", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo DisposedField =
            typeof(DbContext).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic)!;
#endif

        protected DbContextBase() { }
        protected DbContextBase(DbContextOptions options) : base(options) { }

#if NET6_0
        public override void Dispose()
        {
            var hadActiveLease = ((DbContextLease) LeaseField.GetValue(this)!).IsActive;
            base.Dispose();
            if (!hadActiveLease)
                return;
            var hasActiveLease = ((DbContextLease) LeaseField.GetValue(this)!).IsActive;
            if (hasActiveLease)
                return;
            DisposedField.SetValue(this, false);
        }

        public override async ValueTask DisposeAsync()
        {
            var hadActiveLease = ((DbContextLease) LeaseField.GetValue(this)!).IsActive;
            await base.DisposeAsync().ConfigureAwait(false);
            if (!hadActiveLease)
                return;
            var hasActiveLease = ((DbContextLease) LeaseField.GetValue(this)!).IsActive;
            if (hasActiveLease)
                return;
            DisposedField.SetValue(this, false);
        }
#endif
    }
}
