using System;
using Microsoft.EntityFrameworkCore;
using Stl.EntityFramework.Internal;

namespace Stl.EntityFramework
{
    public static class DbContextEx
    {
        private static readonly EventHandler<SavingChangesEventArgs> FailOnSaveChanges =
            (sender, args) => throw Errors.DbContextIsReadOnly();

        // ConfigureMode

        public static void SetAccessMode(this DbContext dbContext, DbAccessMode mode)
        {
            dbContext.EnableChangeTracking(mode == DbAccessMode.ReadWrite);
            dbContext.EnableSaveChanges(mode == DbAccessMode.ReadWrite);
        }

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
