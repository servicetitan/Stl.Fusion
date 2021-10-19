namespace Stl.Tests.Collections;

public class BinaryHeapTest : TestBase
{
    public BinaryHeapTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void EmptyHeapTest()
    {
        var heap = new BinaryHeap<int, int>();
        heap.Count.Should().Be(0);
        heap.PeekMin().IsNone().Should().BeTrue();
        heap.ExtractMin().IsNone().Should().BeTrue();
        Assert.Empty(heap);
    }

    [Fact]
    public void RandomHeapTest()
    {
        var rnd = new Random(10);
        for (var count = 1; count < 100; count++) {
            for (var iteration = 0; iteration < 1000; iteration++) {
                var items = Enumerable.Range(0, count).Select(i => rnd.Next(5)).ToList();
                var heap = new BinaryHeap<int, int>();
                foreach (var item in items)
                    heap.Add(item, item);
                var sortedItems = items.OrderBy(i => i).ToList();
                var heapPriorities = heap.Select(i => i.Priority).ToList();
                var heapValues = heap.Select(i => i.Value).ToList();

                heap.Count.Should().Be(sortedItems.Count);
                heapPriorities.Count.Should().Be(sortedItems.Count);
                heapValues.Count.Should().Be(sortedItems.Count);
                for (var i = 0; i < sortedItems.Count; i++) {
                    heapPriorities[i].Should().Be(sortedItems[i]);
                    heapValues[i].Should().Be(sortedItems[i]);
                }
                var min = heap.ExtractMin();
                min.Value.Priority.Should().Be(sortedItems[0]);
                min.Value.Value.Should().Be(sortedItems[0]);
            }
        }
    }
}
