using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Stl.Fusion.EntityFramework
{
    /// <summary>
    /// This type solves a single problem: currently EF Core 6.0
    /// doesn't properly dispose pooled DbContexts rendering
    /// them unusable after disposal.
    /// Details: https://github.com/dotnet/efcore/issues/26202
    /// </summary>
    public abstract class DbContextBase :
#if NET6_0
        DbContext, IResettableService
#else
        DbContext
#endif
    {
#if NET6_0
        private static readonly FieldInfo DisposedField =
            typeof(DbContext).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo GetResettableServicesMethod =
            typeof(DbContext).GetMethod("GetResettableServices", BindingFlags.Instance | BindingFlags.NonPublic)!;
#endif

        protected DbContextBase() { }
        protected DbContextBase(DbContextOptions options) : base(options) { }

#if NET6_0
        void IResettableService.ResetState()
        {
            var services = GetResettableServices2();
            foreach (var service in services)
                service.ResetState();
            DisposedField.SetValue(this, false);
        }

        async Task IResettableService.ResetStateAsync(CancellationToken cancellationToken = default)
        {
            var services = GetResettableServices2();
            foreach (var service in services)
                await service.ResetStateAsync(cancellationToken).ConfigureAwait(false);
            DisposedField.SetValue(this, false);
        }

        private IEnumerable<IResettableService> GetResettableServices2()
            => (IEnumerable<IResettableService>) GetResettableServicesMethod.Invoke(
                this,
                Array.Empty<object>())!;
#endif
    }
}
