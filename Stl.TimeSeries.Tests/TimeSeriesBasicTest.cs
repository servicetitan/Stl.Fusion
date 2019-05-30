using System;
using System.Linq;
using System.Reactive.Linq;
using Stl.Async;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.TimeSeries.Tests
{
    public class TimeSeriesBasicTest : TestBase
    {
        public TimeSeriesBasicTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BasicTest()
        {
            var sequence1 = Observable.Range(0, 10).ToTimeSeries().ToEnumerable().ToArray();
            var sequence2 = Observable.Range(0, 10).ToTimeSeries().ToEnumerable().ToArray();
            var badItemCount = sequence1
                .Zip(sequence2, 
                     (p1, p2) => p1.Value == p2.Value && (p1.Time - p2.Time) < TimeSpan.FromMilliseconds(50))
                .Count(i => i == false);
            Assert.Equal(0, badItemCount);
        }
    }
}
