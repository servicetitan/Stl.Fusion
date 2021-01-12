using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework
{
    public static class DbContextEx
    {
        private static readonly EventHandler<SavingChangesEventArgs> FailOnSaveChanges =
            (sender, args) => throw Errors.DbContextIsReadOnly();

        // ConfigureMode

        public static TDbContext ReadWrite<TDbContext>(this TDbContext dbContext, bool isReadWrite = true)
            where TDbContext : DbContext
        {
            dbContext.EnableChangeTracking(isReadWrite);
            dbContext.EnableSaveChanges(isReadWrite);
            return dbContext;
        }

        public static TDbContext ReadWrite<TDbContext>(this TDbContext dbContext, bool? isReadWrite)
            where TDbContext : DbContext
            => isReadWrite.HasValue ? dbContext.ReadWrite(isReadWrite.GetValueOrDefault()) : dbContext;

        // (Enable|Disable)ChangeTracking

        public static void EnableChangeTracking(this DbContext dbContext, bool enable)
        {
            if (enable)
                dbContext.EnableChangeTracking();
            else
                dbContext.DisableChangeTracking();
        }

        public static void EnableChangeTracking(this DbContext dbContext)
        {
            var ct = dbContext.ChangeTracker;
            ct.AutoDetectChangesEnabled = true;
            ct.LazyLoadingEnabled = false;
            ct.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        }

        public static void DisableChangeTracking(this DbContext dbContext)
        {
            var ct = dbContext.ChangeTracker;
            ct.AutoDetectChangesEnabled = false;
            ct.LazyLoadingEnabled = false;
            ct.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        // (Enable|Disable)SaveChanges

        public static void EnableSaveChanges(this DbContext dbContext, bool enable)
        {
            if (enable)
                dbContext.EnableSaveChanges();
            else
                dbContext.DisableSaveChanges();
        }

        public static void EnableSaveChanges(this DbContext dbContext)
            => dbContext.SavingChanges -= FailOnSaveChanges;

        public static void DisableSaveChanges(this DbContext dbContext)
            => dbContext.SavingChanges += FailOnSaveChanges;
    }
}
