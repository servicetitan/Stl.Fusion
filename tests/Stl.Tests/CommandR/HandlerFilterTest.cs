using System;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.CommandR;
using Stl.Tests.CommandR.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.CommandR
{
    public class HandlerFilterTest : CommandRTestBase
    {
        public HandlerFilterTest(ITestOutputHelper @out) : base(@out)
        {
            CommandHandlerFilter = (handler, commandType) => !typeof(LogCommand).IsAssignableFrom(commandType);
        }

        [Fact]
        public async Task LogCommandTest()
        {
            var services = CreateServices();
            var command = new LogCommand() { Message = "Hi!" };
            await Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await services.Commander().Call(command);
            });
        }
    }
}
