using Stl.Tests.CommandR.Services;

namespace Stl.Tests.CommandR;

public class BasicTest(ITestOutputHelper @out) : CommandRTestBase(@out)
{
    [Fact]
    public async Task LogCommandTest()
    {
        var services = CreateServices();
        var command = new LogCommand() { Message = "Hi!" };
        await services.Commander().Call(command);
    }

    [Fact]
    public async Task LogEventTest()
    {
        var services = CreateServices();
        var command = new LogEvent() { Message = "Hi!" };
        await services.Commander().Call(command);
    }

    [Fact]
    public async Task DivCommandTest()
    {
        var services = CreateServices();

        var command = new DivCommand() { Divisible = 4, Divisor = 2 };
        var result = await services.Commander().Call(command);
        result.Should().Be(2);
    }

    [Fact]
    public async Task DivByZeroCommandTest()
    {
        var services = CreateServices();

        var command = new DivCommand() { Divisible = 4, Divisor = 0 };
        await Assert.ThrowsAsync<DivideByZeroException>(async () => {
            await services.Commander().Call(command);
        });
    }

    [Fact]
    public async Task MultiChainCommandTest()
    {
        var services = CreateServices();
        var mathService = (MathService)services.GetRequiredService<IMathService>();

        mathService.Value = 0;
        var command = new IncSetFailCommand() {
            SetValue = 2,
            IncrementBy = 1,
            IncrementDelay = 200,
        };
        await services.Commander().Call(command);
        mathService.Value.Should().Be(3);

        mathService.Value = 0;
        command = new IncSetFailCommand() {
            SetValue = 2,
            IncrementBy = 1,
            SetDelay = 200,
        };
        await services.Commander().Call(command);
        mathService.Value.Should().Be(2);

        // Fail early
        command = new IncSetFailCommand() {
            MustFail = true,
            SetDelay = 200,
            IncrementDelay = 200,
        };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await services.Commander().Call(command);
        });

        // Fail late
        command = new IncSetFailCommand() {
            MustFail = true,
            FailDelay = 200,
        };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await services.Commander().Call(command);
        });
    }

    [Fact]
    public async Task RecSumCommandTest()
    {
        var tag = new object();
        RecSumCommand.Tag.Value = tag;

        var services = CreateServices();
        CommandContext.Current.Should().BeNull();
        var result = await services.Commander().Call(new RecSumCommand() {
            Arguments = new double[] {1, 2, 3}
        });
        result.Should().Be(6);

        CommandContext.Current.Should().BeNull();
        result = await services.Commander().Call(new RecSumCommand() {
            Arguments = new double[] {1, 2, 3, 4},
        }, isOutermost: true);
        result.Should().Be(10);

        CommandContext.Current.Should().BeNull();
        RecSumCommand.Tag.Value.Should().Be(tag);
    }
}
