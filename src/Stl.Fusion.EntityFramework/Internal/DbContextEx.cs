using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Stl.Fusion.EntityFramework.Internal
{
    public static class DbContextEx
    {
        private static readonly FieldInfo LeaseField = typeof(DbContext)
            .GetField("_lease", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public static void StopPooling(this DbContext dbContext)
#pragma warning disable EF1001
            => LeaseField.SetValue(dbContext, DbContextLease.InactiveLease);
#pragma warning restore EF1001
    }
}
