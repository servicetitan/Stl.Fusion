using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Stl.Fusion;
using Stl.Tests.Fusion.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Fusion
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class SimplestProviderTest : FusionTestBase, IAsyncLifetime
    {
        public SimplestProviderTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var p = Container.Resolve<ISimplestProvider>();
            p.SetValue("");
            var (gv, gcc) = (p.GetValueCallCount, p.GetCharCountCallCount);

            (await p.GetValueAsync()).Should().Be("");
            (await p.GetCharCountAsync()).Should().Be(0);
            p.GetValueCallCount.Should().Be(++gv);
            p.GetCharCountCallCount.Should().Be(++gcc);

            p.SetValue("1");
            (await p.GetValueAsync()).Should().Be("1");
            (await p.GetCharCountAsync()).Should().Be(1);
            p.GetValueCallCount.Should().Be(++gv);
            p.GetCharCountCallCount.Should().Be(++gcc);

            // Retrying the same - call counts shouldn't change
            (await p.GetValueAsync()).Should().Be("1");
            (await p.GetCharCountAsync()).Should().Be(1);
            p.GetValueCallCount.Should().Be(gv);
            p.GetCharCountCallCount.Should().Be(gcc);
        }

        [Fact]
        public async Task ScopedTest()
        {
            var p = Container.Resolve<ISimplestProvider>();
            p.SetValue("");
            var (gv, gcc) = (p.GetValueCallCount, p.GetCharCountCallCount);
            (await p.GetValueAsync()).Should().Be("");
            (await p.GetCharCountAsync()).Should().Be(0);
            p.GetValueCallCount.Should().Be(++gv);
            p.GetCharCountCallCount.Should().Be(++gcc);

            await using (var s1 = Container.BeginLifetimeScope()) {
                p = s1.Resolve<ISimplestProvider>();
                (gv, gcc) = (p.GetValueCallCount, p.GetCharCountCallCount);
                (await p.GetValueAsync()).Should().Be("");
                (await p.GetCharCountAsync()).Should().Be(0);
                p.GetValueCallCount.Should().Be(gv);
                p.GetCharCountCallCount.Should().Be(gcc);
            }
            await using (var s2 = Container.BeginLifetimeScope()) {
                p = s2.Resolve<ISimplestProvider>();
                (gv, gcc) = (p.GetValueCallCount, p.GetCharCountCallCount);
                (await p.GetValueAsync()).Should().Be("");
                (await p.GetCharCountAsync()).Should().Be(0);
                p.GetValueCallCount.Should().Be(gv);
                p.GetCharCountCallCount.Should().Be(gcc);
            }
        }

        [Fact]
        public async Task ExceptionCachingTest()
        {
            var p = Container.Resolve<ISimplestProvider>();
            p.SetValue("");
            var (gv, gcc) = (p.GetValueCallCount, p.GetCharCountCallCount);

            p.SetValue(null!); // Will cause an exception in GetCharCountAsync
            (await p.GetValueAsync()).Should().Be(null);
            p.GetValueCallCount.Should().Be(++gv);
            p.GetCharCountCallCount.Should().Be(gcc);

            await Assert.ThrowsAsync<NullReferenceException>(() => p.GetCharCountAsync());
            p.GetValueCallCount.Should().Be(gv);
            p.GetCharCountCallCount.Should().Be(++gcc);

            // Exceptions are also cached, so counts shouldn't change here
            await Assert.ThrowsAsync<NullReferenceException>(() => p.GetCharCountAsync());
            p.GetValueCallCount.Should().Be(gv);
            p.GetCharCountCallCount.Should().Be(gcc);

            // But if we wait for 0.1s+, it should recompute again
            await Task.Delay(500);
            await Assert.ThrowsAsync<NullReferenceException>(() => p.GetCharCountAsync());
            p.GetValueCallCount.Should().Be(gv);
            p.GetCharCountCallCount.Should().Be(++gcc);
        }

        [Fact]
        public async Task OptionsTest()
        {
            var d = ComputedOptions.Default;
            var p = Container.Resolve<ISimplestProvider>();
            p.SetValue("");

            var c1 = await Computed.CaptureAsync(_ => p.GetValueAsync());
            c1.Options.KeepAliveTime.Should().Be(d.KeepAliveTime);
            c1.Options.ErrorAutoInvalidateTime.Should().Be(d.ErrorAutoInvalidateTime);
            c1.Options.AutoInvalidateTime.Should().Be(d.AutoInvalidateTime);

            var c2 = await Computed.CaptureAsync(_ => p.GetCharCountAsync());
            c2.Options.KeepAliveTime.Should().Be(d.KeepAliveTime);
            c2.Options.ErrorAutoInvalidateTime.Should().Be(TimeSpan.FromSeconds(0.1));
            c2.Options.AutoInvalidateTime.Should().Be(d.AutoInvalidateTime);
        }
    }
}
