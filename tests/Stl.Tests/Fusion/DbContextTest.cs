using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Stl.Tests.Fusion.Model;
using Stl.Tests.Fusion.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Fusion
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
                Name = "realDonaldTrump"
            };

            var c1 = new Chat() {
                Id = 2,
                Author = u1,
                Title = "Chinese Corona"
            };

            var m1 = new Message() {
                Id = 3,
                Text = "Covfefe",
                Author = u1,
                Chat = c1,

            };

            DbContext.AddRange(u1, c1, m1);
            await DbContext.SaveChangesAsync();

            using var scope = Container.BeginLifetimeScope();
            using var dbContext = scope.Resolve<TestDbContext>();

            (await dbContext.Users.CountAsync()).Should().Be(1);
            (await dbContext.Messages.CountAsync()).Should().Be(1);
            u1 = await dbContext.Users.FindAsync(u1.Id);
            u1.Name.Should().Be("realDonaldTrump");
            
            m1 = await dbContext.Messages
                .Where(p => p.Id == p.Id)
                .Include(p => p.Author)
                .SingleAsync();
            m1.Author.Id.Should().Be(u1.Id);
        }
    }
}
