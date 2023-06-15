using Stl.Fusion.Tests.UIModels;

namespace Stl.Fusion.Tests;

public class ServerTimeModelTest : FusionTestBase
{
    public ServerTimeModelTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task ServerTimeModelTest1()
    {
        await using var serving = await WebHost.Serve();
        using var stm = ClientServices.GetRequiredService<IComputedState<ServerTimeModel1>>();

        var c = stm.Computed;
        c.IsConsistent().Should().BeFalse();
        c.Value.Time.Should().Be(default);

        Debug.WriteLine("0");
        await Update(stm);
        Debug.WriteLine("1");
        await c.Update();
        Debug.WriteLine("2");

        c = stm.Computed;
        c.IsConsistent().Should().BeTrue();
        (DateTime.Now - c.Value.Time).Should().BeLessThan(TimeSpan.FromSeconds(1));

        Debug.WriteLine("3");
        await Task.Delay(TimeSpan.FromSeconds(3));
        Debug.WriteLine("4");
        await Update(stm);
        Debug.WriteLine("5");
        await Task.Delay(300); // Let's just wait for the updates to happen
        Debug.WriteLine("6");
        c = stm.Computed;
        Debug.WriteLine("7");

        // c.IsConsistent.Should().BeTrue(); // Hard to be sure here
        var delta = DateTime.Now - c.Value.Time;
        Debug.WriteLine(delta.TotalSeconds);
        delta.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ServerTimeModelTest2()
    {
        await using var serving = await WebHost.Serve();
        using var stm = ClientServices.GetRequiredService<IComputedState<ServerTimeModel2>>();

        var c = stm.Computed;
        c.IsConsistent().Should().BeFalse();
        c.Value.Time.Should().Be(default);

        Debug.WriteLine("0");
        await Update(stm);
        Debug.WriteLine("1");
        await c.Update();
        Debug.WriteLine("2");

        c = stm.Computed;
        c.IsConsistent().Should().BeTrue();
        (DateTime.Now - c.Value.Time).Should().BeLessThan(TimeSpan.FromSeconds(1));

        Debug.WriteLine("3");
        await Task.Delay(TimeSpan.FromSeconds(3));
        Debug.WriteLine("4");
        await Update(stm);
        Debug.WriteLine("5");
        await Task.Delay(300); // Let's just wait for the updates to happen
        Debug.WriteLine("6");
        c = stm.Computed;
        Debug.WriteLine("7");

        // c.IsConsistent.Should().BeTrue(); // Hard to be sure here
        var delta = DateTime.Now - c.Value.Time;
        Debug.WriteLine(delta.TotalSeconds);
        delta.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    // Private methods

    private static ValueTask<IComputedState<T>> Update<T>(
        IComputedState<T> state, CancellationToken cancellationToken = default)
        => state.Update(cancellationToken);
}
