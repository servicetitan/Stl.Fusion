namespace Stl.Tests.Collections;

public class ArrayBufferTest
{
    private readonly Random _rnd = new Random();

    [Fact]
    public void CombinedTest()
    {
        for (var i1 = 0; i1 < 100; i1++) {
            var list = new List<byte>();
            for (var l = 0; l < 100; l++) {
                list.Add((byte) (_rnd.Next() % 256));
                Test(list);
            }
        }
    }

    private void Test<T>(List<T> list)
    {
        using var buffer = ArrayBuffer<T>.Lease(true);
        foreach (var i in list)
            buffer.Add(i);
        buffer.ToArray().Should().Equal(list);
        buffer.ToList().Should().Equal(list);

        for (var _ = 0; _ < 5; _++) {
            if (buffer.Count == 0)
                break;

            var idx = _rnd.Next(list.Count);
            var item = buffer[idx];
            buffer.RemoveAt(idx);
            list.RemoveAt(idx);
            buffer.ToArray().Should().Equal(list);

            idx = _rnd.Next(list.Count);
            buffer.Insert(idx, item);
            list.Insert(idx, item);
            buffer.ToArray().Should().Equal(list);

            idx = _rnd.Next(list.Count);
            var tmp = buffer[idx];
            buffer.SetItem(idx, list[idx]);
            list[idx] = tmp;
            buffer.ToArray().Should().Equal(list);
        }
    }

    [Fact]
    public void TestEnsureCapacity1()
    {
        var minCapacity = ArrayBuffer<int>.MinCapacity;
        using var b = ArrayBuffer<int>.Lease(true);

        for (var i = 0; i < 3; i++) {
            var capacity = b.Capacity;
            capacity.Should().BeGreaterOrEqualTo(minCapacity);
            var numbers = Enumerable.Range(0, capacity + 1).ToArray();
            b.AddRange(numbers.AsSpan());
            b.Capacity.Should().BeGreaterOrEqualTo(capacity << 1);
        }

        b.Clear();
        b.Capacity.Should().BeGreaterOrEqualTo(minCapacity);

        // Same test, but with .AddRange(IEnumerable<T>)
        for (var i = 0; i < 3; i++) {
            var capacity = b.Capacity;
            capacity.Should().BeGreaterOrEqualTo(minCapacity);
            var numbers = Enumerable.Range(0, capacity + 1);
            b.AddRange(numbers);
            b.Capacity.Should().BeGreaterOrEqualTo(capacity << 1);
        }
    }

    [Fact]
    public void TestEnsureCapacity2()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            using var b = ArrayBuffer<int>.Lease(true);
            b.EnsureCapacity(int.MaxValue);
        });
    }
}
