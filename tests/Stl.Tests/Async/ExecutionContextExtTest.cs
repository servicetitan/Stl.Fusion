using Stl.Testing.Collections;
using Xunit.Sdk;

namespace Stl.Tests.Async;

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously.

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class ExecutionContextExtTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public async Task TaskTest()
    {
        var l = new AsyncLocal<string?>();
        l.Value.Should().BeNull();
        await Task1();
        l.Value.Should().BeNull();
        _ = Task1();
        l.Value.Should().BeNull();
        l.Value = "2";
        l.Value.Should().Be("2");

        using (ExecutionContextExt.TrySuppressFlow()) {
            await Assert.ThrowsAsync<XunitException>(Task1);
            await Assert.ThrowsAsync<XunitException>(() => _ = Task1());
            await Assert.ThrowsAsync<XunitException>(Wrapper);
            await Assert.ThrowsAsync<XunitException>(() => _ = Wrapper());
        }
        await ExecutionContextExt.Start(ExecutionContextExt.Default, Task1).ConfigureAwait(false);
        await ExecutionContextExt.Start(ExecutionContextExt.Default, Wrapper).ConfigureAwait(false);

        async Task Wrapper() {
            await Task1().ConfigureAwait(false);
        }

        async Task Task1() {
            l.Value.Should().BeNull();
            l.Value = "1";
            l.Value.Should().Be("1");
        }
    }

    [Fact]
    public async Task ValueTaskTest()
    {
        var l = new AsyncLocal<string?>();
        l.Value.Should().BeNull();
        await Task1();
        l.Value.Should().BeNull();
        _ = Task1();
        l.Value.Should().BeNull();
        l.Value = "2";
        l.Value.Should().Be("2");

        using (ExecutionContextExt.TrySuppressFlow()) {
            await Assert.ThrowsAsync<XunitException>(() => Task1().AsTask());
            await Assert.ThrowsAsync<XunitException>(() => Wrapper().AsTask());
        }
        await ExecutionContextExt.Start(ExecutionContextExt.Default, () => Task1().AsTask()).ConfigureAwait(false);
        await ExecutionContextExt.Start(ExecutionContextExt.Default, () => Wrapper().AsTask()).ConfigureAwait(false);

        async ValueTask Wrapper() {
            await Task1().ConfigureAwait(false);
        }

        async ValueTask Task1() {
            l.Value.Should().BeNull();
            l.Value = "1";
            l.Value.Should().Be("1");
        }
    }

    [Fact]
    public async Task TrySuppressFlowTest()
    {
        using var _1 = ExecutionContextExt.TrySuppressFlow();
        using var _2 = ExecutionContextExt.TrySuppressFlow();
    }
}
