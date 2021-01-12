using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        private async Task RecAddUsersAsync(
            RecAddUsersCommand command,
            CommandContext context,
            TestDbContext dbContext,
            CancellationToken cancellationToken)
        {
            CommandContext.GetCurrent().Should().Be(context);
            context.ExecutionState.Handlers.Count.Should().Be(3);

            var scopedDbContext = context.Services.GetRequiredService<TestDbContext>();
            scopedDbContext.Should().NotBeNull();
            scopedDbContext.Should().Be(dbContext);

            Log.LogInformation($"User count: {command.Users.Length}");
            if (command.Users.Length == 0)
                return;

            await Services.Commander().CallAsync(
                    new RecAddUsersCommand() { Users = command.Users[1..] },
                    cancellationToken)
                .ConfigureAwait(false);

            var user = command.Users[0];
            if (string.IsNullOrEmpty(user.Id))
                throw new InvalidOperationException("User.Id must be set.");
            await dbContext.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        }
    }
}
