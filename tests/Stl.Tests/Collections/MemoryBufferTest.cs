using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public void TestEnsureCapacity()
        {
            var buffer = MemoryBuffer<byte>.Lease(true);
            buffer.Add(0x01);
            buffer.Add(0x01);
            buffer.Add(0x01);
            buffer.Add(0x01);
            buffer.Add(0x01);

            var baselineCap = buffer.Capacity;
            buffer.EnsureCapacity(baselineCap << 5);//growth exp of 5

            var verifiedCapacity = Math.Log2(buffer.Capacity);
            var baseline = Math.Log2(baselineCap);

            Assert.Equal(5, verifiedCapacity - baseline);
        }

        [Fact]
        public void TestEnsureCapacitySmallerThanCurrent()
        {
            var buffer = MemoryBuffer<byte>.Lease(true);
            buffer.Add(0x01);
            buffer.Add(0x01);
            buffer.Add(0x01);
            buffer.Add(0x01);
            buffer.Add(0x01);

            var baselineCap = buffer.Capacity;
            buffer.EnsureCapacity(2);

            Assert.Equal(baselineCap, buffer.Capacity);
        }

        [Fact]
        public void TestEnsureCapacityError()
        {
            var buffer = MemoryBuffer<byte>.Lease(true);

            try {
                buffer.EnsureCapacity(-1);
                Assert.True(false, "Should've thrown exception");
            }
            catch (InvalidOperationException) {
                Assert.True(true);
            }

            try {
                buffer.EnsureCapacity(int.MaxValue);
                Assert.True(false, "Should've thrown exception");
            }
            catch (InvalidOperationException) {
                Assert.True(true);
            }
        }

        [Fact]
        public void AddRangeTest()
        {
            var buffer = MemoryBuffer<byte>.Lease(true);
            buffer.Add(0x01);
            buffer.Add(0x01);
            buffer.Add(0x01);
            buffer.Add(0x01);
            buffer.Add(0x01);

            var baselineCap = buffer.Capacity;
            var toBeAdded = new List<byte>(1024);// 1024 == 1<<10 and baseline capacity is 1<<3

            for (int i = 0; i < 1024; i++) {
                toBeAdded.Add((byte)(_rnd.Next() % 256));
            }

            buffer.AddRange(toBeAdded);

            var verifiedCapacity = Math.Log2(buffer.Capacity);
            var baseline = Math.Log2(baselineCap);

            Assert.Equal(7, verifiedCapacity - baseline);
        }
    }
}
