using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Internal;
using Xunit;

namespace Stl.Tests.Internal
{
    public class ObjectHolderTest
    {
        [Fact]
        public void BasicTest()
        {
            var holder = new ObjectHolder();
            var objects = Enumerable.Range(0, 10_000).Select(i => i.ToString()).ToArray();

            var holds = new HashSet<IDisposable>();
            for (var i = 0; i < objects.Length; i++)
                holds.Add(holder.Hold(objects[i]));
            holder.IsEmpty.Should().BeFalse();

            // HashSet randomizes the order of disposal
            foreach (var hold in holds)
                hold.Dispose();
            holder.IsEmpty.Should().BeTrue();
        }
    }
}
