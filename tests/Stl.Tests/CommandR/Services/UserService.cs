using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.DependencyInjection;
using Stl.Fusion.EntityFramework;

namespace Stl.Tests.CommandR.Services
{
    [Service, AddCommandHandlers]
    public class UserService : DbServiceBase<TestDbContext>
    {
        public UserService(IServiceProvider services) : base(services) { }

        [CommandHandler]
        private async Task RecAddUsers(
            RecAddUsersCommand command,
            CommandContext context,
            CancellationToken cancellationToken)
        {
            CommandContext.GetCurrent().Should().Be(context);
            context.ExecutionState.Handlers.Count.Should().Be(5);

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
            await using var anotherDbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
            dbContext.Should().NotBeNull();
            anotherDbContext.Should().NotBeNull();
            anotherDbContext.Should().NotBe(dbContext);

            Log.LogInformation("User count: {UserCount}", command.Users.Length);
            if (command.Users.Length == 0)
                return;

            await Services.Commander().Call(
                    new RecAddUsersCommand() { Users = command.Users[1..] },
                    cancellationToken)
                .ConfigureAwait(false);

            var user = command.Users[0];
            if (string.IsNullOrEmpty(user.Id))
                throw new InvalidOperationException("User.Id must be set.");
            await dbContext.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
