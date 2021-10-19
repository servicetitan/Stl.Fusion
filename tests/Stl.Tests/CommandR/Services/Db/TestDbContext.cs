using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Operations;

namespace Stl.Tests.CommandR.Services;

public class TestDbContext : DbContextBase
{
    public DbSet<DbOperation> Operations { get; protected set; } = null!;
    public DbSet<User> Users { get; protected set; } = null!;

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}
