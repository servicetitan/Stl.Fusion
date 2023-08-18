using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication.Services;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Fusion.Extensions.Services;

namespace Stl.Fusion.Tests.Model;

public class DbAuthUser : DbUser<long>
{ }

public class DbAuthSessionInfo : DbSessionInfo<long>
{ }

public class TestDbContext : DbContextBase
{
    public DbSet<User> Users { get; protected set; } = null!;
    public DbSet<Message> Messages { get; protected set; } = null!;
    public DbSet<Chat> Chats { get; protected set; } = null!;

    // Stl.Fusion.EntityFramework tables
    public DbSet<DbOperation> Operations { get; protected set; } = null!;
    public DbSet<DbAuthUser> AuthUsers { get; protected set; } = null!;
    public DbSet<DbUserIdentity<long>> AuthUserIdentities { get; protected set; } = null!;
    public DbSet<DbAuthSessionInfo> AuthSessions { get; protected set; } = null!;
    public DbSet<DbKeyValue> KeyValues { get; protected set; } = null!;

    public TestDbContext(DbContextOptions options) : base(options)
        => this.EnableChangeTracking(false);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>().HasOne(m => m.Author).WithMany()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
