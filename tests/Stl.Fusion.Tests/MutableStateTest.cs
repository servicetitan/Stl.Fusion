using System;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Fusion.UI;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    public class MutableStateTest : TestBase
    {
        public MutableStateTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var ms1 = new MutableState<string>("A");
            ms1.Updated += s => Out.WriteLine($"ms1 = {s.UnsafeValue}");
            ms1.Value.Should().Be("A");

            var ms2 = new MutableState<string>("B");
            ms2.Updated += s => Out.WriteLine($"ms2 = {s.UnsafeValue}");
            ms2.Value.Should().Be("B");

            var c = Computed.New<string>(async ct => {
                var value1 = await ms1.Computed.UseAsync(ct);
                var value2 = await ms2.Computed.UseAsync(ct);
                return $"{value1}{value2}";
            });
            c = await c.UpdateAsync(false);
            c.Value.Should().Be("AB");

            ms1.Value = "X";
            ms1.Value.Should().Be("X");
            c = await c.UpdateAsync(false);
            c.Value.Should().Be("XB");

            ms2.Value = "Y";
            ms2.Value.Should().Be("Y");
            c = await c.UpdateAsync(false);
            c.Value.Should().Be("XY");

            ms1.Error = new NullReferenceException();
            ms1.HasError.Should().BeTrue();
            ms1.HasValue.Should().BeFalse();
            ms1.Error.Should().BeOfType<NullReferenceException>();
            c = await c.UpdateAsync(false);
            c.HasError.Should().BeTrue();
            c.HasValue.Should().BeFalse();
            c.Error.Should().BeOfType<NullReferenceException>();
        }
    }
}
