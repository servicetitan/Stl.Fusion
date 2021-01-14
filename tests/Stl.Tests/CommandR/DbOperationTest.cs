using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.Fusion.EntityFramework;
using Stl.Tests.CommandR.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.CommandR
{
    public class DbOperationTest : CommandRTestBase
    {
        public DbOperationTest(ITestOutputHelper @out) : base(@out)
        {
            UseDbContext = true;
        }

        [Fact]
        public async Task RecAddUsersTest()
        {
            var services = CreateServices();
            var command = new RecAddUsersCommand() { Users = new [] {
                new User() { Id = "a", Name = "Alice" },
                new User() { Id = "b", Name = "Bob" },
            }};
            await services.Commander().CallAsync(command);

            var f = services.GetRequiredService<IDbContextFactory<TestDbContext>>();
            await using var dbContext = f.CreateDbContext().ReadWrite(false);
            (await dbContext.Users.AsQueryable().CountAsync()).Should().Be(2);
            (await dbContext.Operations.AsQueryable().CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task RecAddUserFailTest()
        {
            var services = CreateServices();
            var command = new RecAddUsersCommand() { Users = new [] {
                new User() { Id = "a", Name = "Alice" },
                new User() { Id = "", Name = "Fail" },
                new User() { Id = "b", Name = "Bob" },
            }};
            await Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await services.Commander().CallAsync(command);
            });

            var f = services.GetRequiredService<IDbContextFactory<TestDbContext>>();
            await using var dbContext = f.CreateDbContext().ReadWrite(false);
            (await dbContext.Users.AsQueryable().CountAsync()).Should().Be(0);
            (await dbContext.Operations.AsQueryable().CountAsync()).Should().Be(0);
        }
    }
}
