using Stl.Tests.CommandR.Services;

namespace Stl.Tests.CommandR;

public class HandlerFilterTest : CommandRTestBase
{
    public HandlerFilterTest(ITestOutputHelper @out) : base(@out)
    {
        CommandHandlerFilter = (_, commandType) =>
            !typeof(LogCommand).IsAssignableFrom(commandType)
            && !typeof(LogEvent).IsAssignableFrom(commandType);
    }

    [Fact]
    public async Task LogCommandTest()
    {
        var services = CreateServices();
        var command = new LogCommand() { Message = "Hi!" };
        // Should fail: any command should have at least one final handler
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await services.Commander().Call(command);
        });
    }

    [Fact]
    public async Task LogEventTest()
    {
        var services = CreateServices();
        var command = new LogEvent() { Message = "Hi!" };
        // Should not fail: events may have no handlers
        await services.Commander().Call(command);
    }
}
