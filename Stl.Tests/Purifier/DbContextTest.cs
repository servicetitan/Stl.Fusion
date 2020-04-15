using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Stl.Tests.Purifier.Model;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Purifier
{
    public class DbContextTest : PurifierTestBase, IAsyncLifetime
    {
        public DbContextTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var count = await DbContext.Users.CountAsync();
            count.Should().Be(0);

            var u1 = new User() {
                Id = 1,
                Name = "AY"
            };
            var p1 = new Post() {
                Id = 2,
                Title = "Test",
                Author = u1,
            };
            await DbContext.Users.AddAsync(u1);
            await DbContext.Posts.AddAsync(p1);
            await DbContext.SaveChangesAsync();

            using var scope = Container.BeginLifetimeScope();
            using var dbContext = scope.Resolve<TestDbContext>();

            (await dbContext.Users.CountAsync()).Should().Be(1);
            (await dbContext.Posts.CountAsync()).Should().Be(1);
            u1 = await dbContext.Users.FindAsync(u1.Id);
            u1.Name.Should().Be("AY");
            p1 = await dbContext.Posts
                .Where(p => p.Id == p.Id)
                .Include(p => p.Author)
                .SingleAsync();
            p1.Author.Id.Should().Be(u1.Id);
            // u.Posts.Count().Should().Be(1);
        }
    }
}
