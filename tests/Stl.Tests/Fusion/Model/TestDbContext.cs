using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Stl.OS;
using Stl.Pooling;

namespace Stl.Tests.Fusion.Model
{
    public class TestDbContextPool : ScopedServicePool<TestDbContext>
    {
        public TestDbContextPool(IServiceProvider services)
            : base(services, CanReuse, HardwareInfo.ProcessorCount * 64) { }

        private static bool CanReuse(TestDbContext dbContext)
#pragma warning disable EF1001
            => !dbContext.ChangeTracker.Entries().Any();
#pragma warning restore EF1001
    }

    public class TestDbContext : DbContext
    {
        public DbSet<User> Users { get; protected set; } = null!;
        public DbSet<Message> Messages { get; protected set; } = null!;
        public DbSet<Chat> Chats { get; protected set; } = null!;

        public TestDbContext(DbContextOptions options) : base(options)
        {
            var ct = ChangeTracker;
            ct.AutoDetectChangesEnabled = false;
            ct.LazyLoadingEnabled = false;
            ct.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var user = modelBuilder.Entity<User>();
            user.HasIndex(u => u.Name);

            var message = modelBuilder.Entity<Message>();
            message.HasIndex(c => c.Date);

            var chat = modelBuilder.Entity<Chat>();
            chat.HasIndex(c => c.Title);
        }
    }
}
