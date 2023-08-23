namespace Stl.Tests.CommandR;

#pragma warning disable MA0012

public class LocalCommandTest(ITestOutputHelper @out) : CommandRTestBase(@out)
{
    [Fact]
    public async Task BasicTest()
    {
        var services = CreateServices();
        var commander = services.Commander();
        var count = 0;

        (await commander.Call(LocalCommand.New(() => ++count))).Should().Be(1);
        count.Should().Be(1);

        (await commander.Call(LocalCommand.New(async _ => {
            await Task.Delay(10);
            return ++count;
        }))).Should().Be(2);
        count.Should().Be(2);

        await commander.Call(LocalCommand.New((Action) (() => ++count)));
        count.Should().Be(3);

        await commander.Call(LocalCommand.New(_ => {
            ++count;
            return Task.CompletedTask;
        }));
        count.Should().Be(4);
    }

    [Fact]
    public async Task ExceptionTest()
    {
        var services = CreateServices();
        var commander = services.Commander();
        var count = 0;
        count.Should().Be(0);

        var context = await commander.Run(LocalCommand.New(() => throw new NullReferenceException()));
        context.UntypedResult.Error!.GetBaseException().Should().BeOfType<NullReferenceException>();

        context = await commander.Run(LocalCommand.New(async _ => {
            await Task.Delay(10);
            throw new NullReferenceException();
#pragma warning disable 162
            return 1;
#pragma warning restore 162
        }));
        context.UntypedResult.Error!.GetBaseException().Should().BeOfType<NullReferenceException>();
    }
}
