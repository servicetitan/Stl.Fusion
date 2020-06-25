using System;
using FluentAssertions;
using Stl.Time;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Hosting
{
    public class BasicMiniHostTest : MiniHostTestBase
    {
        public BasicMiniHostTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BasicTest()
        {
            Host.Should().NotBeNull();
            (Clock.Now - SystemClock.Now).Should().BeLessThan(TimeSpan.FromSeconds(1));
        }
    }
}
