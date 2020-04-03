using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stl.Testing;
using Stl.Tests.Purifier.Model;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Purifier
{
    public class PurifierTest : TestBase, IAsyncLifetime
    {
        public TestDbContext DbContext { get; set; }

        public PurifierTest(ITestOutputHelper @out) : base(@out)
        {
            DbContext = CreateDbContext();
        }

        public Task InitializeAsync() 
            => DbContext.Database.EnsureCreatedAsync();
        public Task DisposeAsync() 
            => Task.CompletedTask;

        public TestDbContext CreateDbContext() 
            => new TestDbContext(new DbContextOptionsBuilder()
                .LogTo(Out.WriteLine, LogLevel.Information)
                .Options);

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
            u1.Posts.Add(p1.Key, p1);
            await DbContext.Users.AddAsync(u1);
            await DbContext.Posts.AddAsync(p1);
            await DbContext.SaveChangesAsync();

            DbContext = CreateDbContext();
            (await DbContext.Users.CountAsync()).Should().Be(1);
            (await DbContext.Posts.CountAsync()).Should().Be(1);
            u1 = await DbContext.Users.FindAsync(u1.Id);
            u1.Name.Should().Be("AY");
            p1 = await DbContext.Posts
                .Where(p => p.Id == p.Id)
                .Include(p => p.Author)
                .SingleAsync();
            p1.Author.Id.Should().Be(u1.Id);
            // u.Posts.Count().Should().Be(1);
        }
    }
}
