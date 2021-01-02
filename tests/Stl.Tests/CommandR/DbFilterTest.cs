using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.EntityFramework;
using Stl.Tests.CommandR.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.CommandR
{
    public class DbFilterTest : CommandRTestBase
    {
        public DbFilterTest(ITestOutputHelper @out) : base(@out)
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
            await services.CommandDispatcher().RunAsync(command);

            var tx = services.GetRequiredService<IDbTransactionRunner<TestDbContext>>();
            await tx.ReadAsync(async dbContext => {
                dbContext.Users.Count().Should().Be(2);
                dbContext.Operations.Count().Should().Be(1);
            });
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
            await services.CommandDispatcher().RunAsync((ICommand) command);

            var tx = services.GetRequiredService<IDbTransactionRunner<TestDbContext>>();
            await tx.ReadAsync(async dbContext => {
                dbContext.Users.Count().Should().Be(0);
                dbContext.Operations.Count().Should().Be(0);
            });
        }
    }
}
