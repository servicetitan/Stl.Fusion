using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Operations;

namespace Stl.Tests.CommandR.Services;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContextBase(options)
{
    public DbSet<DbOperation> Operations { get; protected set; } = null!;
    public DbSet<User> Users { get; protected set; } = null!;
}
