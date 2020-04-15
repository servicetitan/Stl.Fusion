using System;
using System.Threading.Tasks;
using Autofac;
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
        public async Task BasicContainerTest()
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
    }
}
