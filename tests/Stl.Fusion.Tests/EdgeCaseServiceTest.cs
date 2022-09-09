using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Tests.Services;
using Stl.Interception;

namespace Stl.Fusion.Tests;

public class EdgeCaseServiceTest : FusionTestBase
{
    public EdgeCaseServiceTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

    [Fact(Timeout = 30_000)]
    public async Task TestService()
    {
        // await using var serving = await WebSocketHost.Serve();
        var service = Services.GetRequiredService<IEdgeCaseService>();
        await ActualTest(service);
    }

    [Fact(Timeout = 30_000)]
    public async Task TestClient()
    {
        await using var serving = await WebHost.Serve();
        var client = ClientServices.GetRequiredService<IEdgeCaseClient>();
        var tfv = ClientServices.TypeViewFactory<IEdgeCaseService>();
        var service = tfv.CreateView(client);
        await ActualTest(service);
    }

    [Fact(Timeout = 300_000)]
    public async Task TestNullable()
    {
        await using var serving = await WebHost.Serve();
        var client = ClientServices.GetRequiredService<IEdgeCaseClient>();
        var tfv = ClientServices.TypeViewFactory<IEdgeCaseService>();
        var service = tfv.CreateView(client);
        var actualService = WebServices.GetRequiredService<IEdgeCaseService>();

        (await service.GetNullable(1)).Should().Be((long?) 1);
        using (Computed.Invalidate())
            _ = actualService.GetNullable(1);
        await Delay(0.2);
        (await service.GetNullable(1)).Should().Be((long?) 1);

        var c = await Computed.Capture(() => service.GetNullable(0));
        c.Value.Should().Be(null);
        using (Computed.Invalidate())
            _ = actualService.GetNullable(0);
        await Delay(0.2);
        c.IsConsistent().Should().BeFalse();
        using var cts = new CancellationTokenSource(1000);
        var c1 = await c.Update(cts.Token);
        c1.Version.Should().NotBe(c.Version);
        c1.Value.Should().Be(null);
    }

    private async Task ActualTest(IEdgeCaseService service)
    {
        var error = (Exception?) null;
        await service.SetSuffix("");
        (await service.GetSuffix()).Should().Be("");

        // ThrowIfContainsError method test
        var c1 = await Computed.Capture(() => service.ThrowIfContainsError("a"));
        c1.Value.Should().Be("a");

        var c2 = await Computed.Capture(() => service.ThrowIfContainsError("error"));
        c2.Error.Should().BeAssignableTo<ArgumentException>();
        c2.Error!.Message.Should().StartWith("Error!");

        await service.SetSuffix("z");
        c1 = await Update(c1);
        c1.Value.Should().Be("az");

        c2 = await Update(c2);
        c2.Error.Should().BeAssignableTo<ArgumentException>();
        c2.Error!.Message.Should().StartWith("Error!");
        await service.SetSuffix("");

        // ThrowIfContainsErrorNonCompute method test
        (await service.ThrowIfContainsErrorNonCompute("a")).Should().Be("a");
        try {
            await service.ThrowIfContainsErrorNonCompute("error");
        } catch (Exception e) { error = e; }
        c2.Error.Should().BeAssignableTo<ArgumentException>();
        c2.Error!.Message.Should().StartWith("Error!");

        await service.SetSuffix("z");
        (await service.ThrowIfContainsErrorNonCompute("a")).Should().Be("az");
        try {
            await service.ThrowIfContainsErrorNonCompute("error");
        } catch (Exception e) { error = e; }
        c2.Error.Should().BeAssignableTo<ArgumentException>();
        c2.Error!.Message.Should().StartWith("Error!");
        await service.SetSuffix("");
    }

    private async Task<Computed<T>> Update<T>(Computed<T> computed, CancellationToken cancellationToken = default)
    {
        if (computed is IReplicaMethodComputed rc)
            await rc.Replica!.RequestUpdate(cancellationToken);
        return await computed.Update(cancellationToken);
    }
}
