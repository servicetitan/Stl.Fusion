using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;

namespace Stl.Tests.CommandR.Services
{
    public class TestDbContext : DbContext
    {
        public DbSet<DbOperation> Operations { get; protected set; } = null!;
        public DbSet<User> Users { get; protected set; } = null!;

        public TestDbContext(DbContextOptions options) : base(options) { }
    }
}
