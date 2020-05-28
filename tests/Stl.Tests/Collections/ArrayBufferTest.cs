using System;
using System.Collections.Generic;
using FluentAssertions;
using Stl.Collections;
using Xunit;

namespace Stl.Tests.Collections
{
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
            var buffer = MemoryBuffer<T>.Lease();
            try {
                foreach (var i in list) 
                    buffer.Add(i);
                buffer.ToArray().Should().Equal(list);

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
                    buffer[idx] = buffer[idx];
                    buffer.ToArray().Should().Equal(list);
                }
            }
            finally {
                buffer.Release();
            }
        }
    }
}
