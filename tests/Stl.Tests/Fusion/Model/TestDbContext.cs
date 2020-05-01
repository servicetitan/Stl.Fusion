using System;
using Microsoft.EntityFrameworkCore;

namespace Stl.Tests.Fusion.Model
{
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
