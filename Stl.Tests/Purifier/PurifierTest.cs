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

            var u = new User() {
                Id = 1,
                Name = "AY"
            };
            await DbContext.Users.AddAsync(u);
            await DbContext.SaveChangesAsync();

            count = await DbContext.Users.CountAsync();
            count.Should().Be(1);
        }
    }
}
