using System.Collections.Concurrent;

namespace Stl.Tests.Collections;

public class ConcurrentDictionaryExtText
{
    [Fact]
    public void GetCapacityTest()
    {
        var c = new ConcurrentDictionary<int, int>(1, 100);
        c.GetCapacity().Should().BeGreaterOrEqualTo(100);
        c = new ConcurrentDictionary<int, int>(1, 10_000);
        c.GetCapacity().Should().BeGreaterOrEqualTo(10_000);
    }
}
