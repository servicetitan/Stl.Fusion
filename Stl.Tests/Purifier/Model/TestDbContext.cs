using System;
using Microsoft.EntityFrameworkCore;

namespace Stl.Tests.Purifier.Model
{
    public class TestDbContext : DbContext
    {
        public DbSet<User> Users { get; protected set; } = null!;
        public DbSet<Post> Posts { get; protected set; } = null!;
        
        public TestDbContext(DbContextOptions options) : base(options)
        {
            var ct = ChangeTracker;
            ct.AutoDetectChangesEnabled = false;
            ct.LazyLoadingEnabled = false;
            ct.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseInMemoryDatabase(GetType().FullName);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var user = modelBuilder.Entity<User>();
            user.HasIndex(u => u.Name);
        }
    }
}
