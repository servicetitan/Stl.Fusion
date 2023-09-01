namespace Stl.Tests.Collections;

public class RingBufferTest
{
    private readonly Random _rnd = new();

    [Fact]
    public void CombinedTest()
    {
        for (var iteration = 0; iteration < 10; iteration++) {
            for (var length = 0; length < 50; length++) {
                var list = new List<byte>(length);
                for (var i = 0; i < length; i++) {
                    list.Add(NextItem());
                    Test(list);
                }
            }
        }
    }

    private void Test<T>(List<T> list)
    {
        var buffer = new RingBuffer<T>(list.Count);
        foreach (var i in list)
            buffer.PushTail(i);
        buffer.ToArray().Should().Equal(list);
        buffer.ToList().Should().Equal(list);

        for (var _ = 0; _ < 10; _++) {
            if (buffer.IsEmpty) {
                buffer.TryPullHead(out var _).Should().BeFalse();
                buffer.TryPullTail(out var _).Should().BeFalse();
                break;
            }

            var idx = _rnd.Next(buffer.Count);
            var item = buffer[idx];
            buffer[idx] = item;
            if (_rnd.Next(2) == 0) {
                buffer.PullHead();
                buffer.PushHead(item);
                list.RemoveAt(0);
                list.Insert(0, item);
            }
            else {
                buffer.PullTail();
                buffer.PushTail(item);
                list.RemoveAt(list.Count - 1);
                list.Add(item);
            }
            buffer.ToArray().Should().Equal(list);
        }
    }

    private byte NextItem()
        => (byte)(_rnd.Next() % 256);
}
