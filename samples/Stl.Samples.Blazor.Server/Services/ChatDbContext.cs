using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Stl.OS;
using Stl.Pooling;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Server.Services
{
    public class ChatDbContextPool : ScopedServicePool<ChatDbContext>
    {
        public ChatDbContextPool(IServiceProvider services) 
            : base(services, CanReuse, HardwareInfo.ProcessorCount * 4) { }

        private static bool CanReuse(ChatDbContext dbContext)
#pragma warning disable EF1001
            => !dbContext.ChangeTracker.Entries().Any();
#pragma warning restore EF1001
    }

    public class ChatDbContext : DbContext
    {
        public DbSet<ChatUser> Users { get; protected set; } = null!;
        public DbSet<ChatMessage> Messages { get; protected set; } = null!;
        
        public ChatDbContext(DbContextOptions options) : base(options)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            var ct = ChangeTracker;
            ct.AutoDetectChangesEnabled = false;
            ct.LazyLoadingEnabled = false;
            ct.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var user = modelBuilder.Entity<ChatUser>();
            user.HasIndex(u => u.Name);

            var message = modelBuilder.Entity<ChatMessage>();
            message.HasIndex(m => m.UserId);
            message.HasIndex(m => m.CreatedAt);
        }
    }
}
