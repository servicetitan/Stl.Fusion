using System;
using System.Collections.Generic;
using FluentAssertions;
using Stl.Collections;
using Xunit;

namespace Stl.Tests.Collections
{
    public class ZListTest
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
            using var lease = ZList<T>.Rent();
            var zList = lease.List;

            foreach (var i in list) zList.Add(i);
            zList.Should().Equal(list);

            for (var _ = 0; _ < 5; _++) {
                if (zList.Count == 0)
                    break;

                var idx = _rnd.Next(list.Count);
                var item = zList[idx];
                zList.RemoveAt(idx);
                list.RemoveAt(idx);
                zList.Should().Equal(list);

                idx = _rnd.Next(list.Count);
                zList.Insert(idx, item);
                list.Insert(idx, item);
                zList.Should().Equal(list);

                idx = _rnd.Next(list.Count);
                zList[idx] = zList[idx];
                zList.Should().Equal(list);
            }
        }
    }
}
