using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Stl.Collections;
using Xunit;

namespace Stl.Tests.Collections
{
    public class MemoryBufferTest
    {
        private readonly Random _rnd = new Random();

        [Fact]
        public void CombinedTest()
        {
            for (var i1 = 0; i1 < 100; i1++) {
                var list = new List<byte>();
                for (var l = 0; l < 100; l++) {
                    list.Add((byte)(_rnd.Next() % 256));
                    Test(list);
                }
            }
        }

        private void Test<T>(List<T> list)
        {
            var buffer = MemoryBuffer<T>.Lease(true);
            try {
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
                    buffer[idx] = list[idx];
                    list[idx] = tmp;
                    buffer.ToArray().Should().Equal(list);
                }
            }
            finally {
                buffer.Release();
            }
        }

        [Fact]
        public void TestEnsureCapacity1()
        {
            var minCapacity = MemoryBuffer<int>.MinCapacity;
            var b = MemoryBuffer<int>.Lease(true);
            try {
                for (var i = 0; i < 3; i++) {
                    var capacity = b.Capacity;
                    capacity.Should().BeGreaterOrEqualTo(minCapacity);
                    var numbers = Enumerable.Range(0, capacity + 1).ToArray();
                    b.AddRange(numbers);
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
            finally {
                b.Release();
            }
        }

        [Fact]
        public void TestEnsureCapacity2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var b = MemoryBuffer<int>.Lease(true);
                try {
                    b.EnsureCapacity(int.MaxValue);
                }
                finally {
                    b.Release();
                }
            });
        }
    }
}
