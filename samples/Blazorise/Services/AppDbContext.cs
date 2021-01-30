using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Authentication;
using Stl.Fusion.EntityFramework.Operations;

namespace Templates.Blazor2.Services
{
    public class AppDbContext : DbContext
    {
        // Stl.Fusion.EntityFramework tables
        public DbSet<DbOperation> Operations { get; protected set; } = null!;
        public DbSet<DbSessionInfo> Sessions { get; protected set; } = null!;
        public DbSet<DbUser> Users { get; protected set; } = null!;
        public DbSet<DbUserIdentity> UserIdentities { get; protected set; } = null!;

        public AppDbContext(DbContextOptions options) : base(options) { }
    }
}
