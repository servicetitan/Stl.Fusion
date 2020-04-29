using System;
using System.Linq;
using Stl.Collections;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Collections
{
    public class BinaryHeapTest : TestBase
    {
        public BinaryHeapTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void EmptyHeapTest()
        {
            var heap = new BinaryHeap<int>();
            Assert.Equal(0, heap.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => heap.Min);
            Assert.Throws<ArgumentOutOfRangeException>(() => heap.RemoveMin());
            Assert.Empty(heap);
        }
        
        [Fact]
        public void RandomHeapTest()
        {
            var rnd = new Random(10);
            for (var count = 1; count < 10; count++) {
                for (var iteration = 0; iteration < 10000; iteration++) {
                    var items = Enumerable.Range(0, count).Select(i => rnd.Next(5)).ToList();
                    var heap = new BinaryHeap<int>();
                    foreach (var item in items)
                        heap.Add(item);
                    var sortedItems = items.OrderBy(i => i).ToList();
                    var heapItems = heap.ToList();
                    Assert.Equal(sortedItems.Count, heap.Count);
                    Assert.Equal(sortedItems.Count, heapItems.Count);
                    for (var i = 0; i < sortedItems.Count; i++)
                        Assert.Equal(sortedItems[i], heapItems[i]);
                    Assert.Equal(sortedItems[0], heap.Min);
                }
            }
        }
    }
}
