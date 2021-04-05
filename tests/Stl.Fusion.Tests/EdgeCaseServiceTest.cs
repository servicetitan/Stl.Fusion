using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Tests.Services;
using Stl.Interception;
using Stl.Interception.Interceptors;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class EdgeCaseServiceTest : FusionTestBase
    {
        private Type ThrowIfContainsErrorExceptionType { get; set; } =
            typeof(ArgumentException);
        private Type ThrowIfContainsErrorRewriteErrorsExceptionType { get; set; } =
            typeof(ServiceException);
        private Type ThrowIfContainsErrorNonComputeExceptionType { get; set; } =
            typeof(ArgumentException);

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
            var tfv = ClientServices.GetTypeViewFactory<IEdgeCaseService>();
            var service = tfv.CreateView(client);
            await ActualTest(service);
        }

        [Fact(Timeout = 30_000)]
        public async Task TestRewriteClient()
        {
            await using var serving = await WebHost.Serve();
            var client = ClientServices.GetRequiredService<IEdgeCaseRewriteClient>();
            var tfv = ClientServices.GetTypeViewFactory<IEdgeCaseService>();
            var service = tfv.CreateView(client);

            // ReSharper disable once SuspiciousTypeConversion.Global
            (service is TypeView<IEdgeCaseService>).Should().BeTrue();
            // ReSharper disable once SuspiciousTypeConversion.Global
            (service is TypeView<IEdgeCaseRewriteClient, IEdgeCaseService>).Should().BeTrue();

            // This part tests that proxy builder generates
            // a different type for each combination of <TView, TImplementation>
            var otherClient = ClientServices.GetRequiredService<IEdgeCaseClient>();
            var otherService = tfv.CreateView(otherClient);
            service.GetType().Should().NotBeSameAs(otherService.GetType());

            ThrowIfContainsErrorExceptionType = typeof(ServiceException);
            ThrowIfContainsErrorNonComputeExceptionType = typeof(ServiceException);
            ThrowIfContainsErrorRewriteErrorsExceptionType = typeof(ServiceException);

            await ActualTest(service);
        }

        private async Task ActualTest(IEdgeCaseService service)
        {
            var error = (Exception?) null;
            await service.SetSuffix("");
            (await service.GetSuffix()).Should().Be("");

            // ThrowIfContainsError method test
            var c1 = await Computed.Capture(
                ct => service.ThrowIfContainsError("a", ct));
            c1.Value.Should().Be("a");

            var c2 = await Computed.Capture(
                ct => service.ThrowIfContainsError("error", ct));
            c2.Error!.GetType().Should().Be(ThrowIfContainsErrorExceptionType);
            c2.Error.Message.Should().Be("!");

            await service.SetSuffix("z");
            c1 = await Update(c1);
            c1.Value.Should().Be("az");

            c2 = await Update(c2);
            c2.Error!.GetType().Should().Be(ThrowIfContainsErrorExceptionType);
            c2.Error.Message.Should().Be("!");
            await service.SetSuffix("");

            // ThrowIfContainsErrorRewriteErrors method test
            c1 = await Computed.Capture(
                ct => service.ThrowIfContainsErrorRewriteErrors("a", ct));
            c1.Value.Should().Be("a");

            c2 = await Computed.Capture(
                ct => service.ThrowIfContainsErrorRewriteErrors("error", ct));
            c2.Error!.GetType().Should().Be(ThrowIfContainsErrorRewriteErrorsExceptionType);
            c2.Error.Message.Should().Be("!");

            await service.SetSuffix("z");
            c1 = await Update(c1);
            c1.Value.Should().Be("az");

            c2 = await Update(c2);
            c2.Error!.GetType().Should().Be(ThrowIfContainsErrorRewriteErrorsExceptionType);
            c2.Error.Message.Should().Be("!");
            await service.SetSuffix("");

            // ThrowIfContainsErrorRewriteErrors method test
            (await service.ThrowIfContainsErrorNonCompute("a")).Should().Be("a");
            try {
                await service.ThrowIfContainsErrorNonCompute("error");
            } catch (Exception e) { error = e; }
            error!.GetType().Should().Be(ThrowIfContainsErrorNonComputeExceptionType);
            error.Message.Should().Be("!");

            await service.SetSuffix("z");
            (await service.ThrowIfContainsErrorNonCompute("a")).Should().Be("az");
            try {
                await service.ThrowIfContainsErrorNonCompute("error");
            } catch (Exception e) { error = e; }
            error!.GetType().Should().Be(ThrowIfContainsErrorNonComputeExceptionType);
            error.Message.Should().Be("!");
            await service.SetSuffix("");
        }

        private async Task<IComputed<T>> Update<T>(IComputed<T> computed, CancellationToken cancellationToken = default)
        {
            if (computed is IReplicaMethodComputed rc)
                await rc.Replica!.RequestUpdate(cancellationToken);
            return await computed.Update(false, cancellationToken);
        }
    }
}
