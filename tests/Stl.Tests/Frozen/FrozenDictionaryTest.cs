using System.Collections.Generic;
using FluentAssertions;
using Stl.Frozen;
using Xunit;

namespace Stl.Tests.Frozen
{
    public class FrozenDictionaryTest
    {
        [Fact]
        public void BasicTest()
        {
            var d1 = new FrozenDictionary<int, int>();
            d1.Count.Should().Be(0);
            d1.ThrowIfFrozen();
            d1.Remove(1).Should().BeFalse();
            d1.Freeze();
            d1.ThrowIfNotFrozen();

            d1 = d1.ToUnfrozen();
            d1.Count.Should().Be(0);
            d1.ThrowIfFrozen();

            d1.Add(1, 2);
            d1.Count.Should().Be(1);
            d1.ContainsKey(1).Should().BeTrue();
            d1[1].Should().Be(2);
            d1[1] = 2;
            d1.Count.Should().Be(1);
            d1.ContainsKey(0).Should().BeFalse();
            d1.ContainsKey(1).Should().BeTrue();
            d1[1].Should().Be(2);
            d1.Freeze();
            d1.ThrowIfNotFrozen();
            d1 = d1.ToUnfrozen();

            d1.Remove(1).Should().BeTrue();
            d1.Add(new KeyValuePair<int, int>(1, 2));
            d1.Add(new KeyValuePair<int, int>(3, 4));
            d1.Count.Should().Be(2);
            d1.ContainsKey(0).Should().BeFalse();
            d1.ContainsKey(1).Should().BeTrue();
            d1.ContainsKey(3).Should().BeTrue();
            d1[1].Should().Be(2);
            d1[3].Should().Be(4);
            d1[1] = 5;
            d1[1].Should().Be(5);
            d1.Count.Should().Be(2);
            d1.Freeze();
            d1.ThrowIfNotFrozen();

            d1 = d1.ToUnfrozen();
            d1.Count.Should().Be(2);
        }
    }
}
