using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Tests.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    public class ListArgumentTest : SimpleFusionTestBase
    {
        public ListArgumentTest(ITestOutputHelper @out) : base(@out) { }

        protected override void ConfigureCommonServices(ServiceCollection services)
            => services.AddFusion().AddAuthentication();

        [Fact]
        public async Task BasicTest()
        {
            var services = CreateServiceProviderFor<MathService>();
            var math = services.GetRequiredService<MathService>();
            var allComputed = new HashSet<IComputed>();

            var c1 = await Computed.CaptureAsync(_ => math.SumAsync(null));
            c1.Value.Should().Be(0);
            allComputed.Add(c1);
            var c2 = await Computed.CaptureAsync(_ => math.SumAsync(null));
            c2.Should().BeSameAs(c1);

            for (var i = 0; i < 20; i++) {
                var values = Enumerable.Range(0, i).ToArray();
                c1 = await Computed.CaptureAsync(_ => math.SumAsync(values));
                c1.Value.Should().Be(values.Sum());
                allComputed.Add(c1);
                c2 = await Computed.CaptureAsync(_ => math.SumAsync(values));
                c2.Should().BeSameAs(c1);
            }

            allComputed.Count.Should().Be(21);
        }
    }
}
