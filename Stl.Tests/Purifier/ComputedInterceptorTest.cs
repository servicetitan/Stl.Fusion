using System;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Purifier;
using Stl.Tests.Purifier.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Purifier
{
    public class ComputedInterceptorTest : PurifierTestBase, IAsyncLifetime
    {
        public ComputedInterceptorTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var c = CreateServices().GetRequiredService<ILifetimeScope>();
            var tp = c.Resolve<ITimeProvider>();                                                      
            var tpe = c.Resolve<ITimeProviderEx>();
            var cNow = await tpe.GetTimeAsync();
            using (var o = cNow.AutoRecompute()) {
                using var _ = o.Subscribe(c => Out.WriteLine($"-> {c.Value}"));
                await Task.Delay(2000);
            }
            Out.WriteLine("Disposed.");
            await Task.Delay(2000);
            Out.WriteLine("Finished.");
        }

        [Fact]
        public async Task CachingTest()
        {
            var c = CreateServices().GetRequiredService<ILifetimeScope>();
            var tpe = c.Resolve<ITimeProviderEx>();
            var cNowOld = tpe.GetTimeAsync();
            await Task.Delay(500);
            var cNow1 = await tpe.GetTimeAsync();
            cNow1.Should().NotBe(cNowOld);
            var cNow2 = await tpe.GetTimeAsync();
            cNow2.Should().Be(cNow1);
        }
    }
}
