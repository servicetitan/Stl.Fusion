using System;
using System.Collections.Generic;
using System.Linq;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests
{
    public class EnumerableExTest : TestBase
    {
        public EnumerableExTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void OrderByDependencyTest()
        {
            IEnumerable<char> DepSelector1(char c) => 
                Enumerable
                    .Range(0, c - '0')
                    .Select(i => (char) ('0' + i));
            IEnumerable<char> BadDepSelector1(char c) => new [] {c};
            IEnumerable<char> BadDepSelector2(char c) => 
                Enumerable
                    .Range(1, 5)
                    .Select(i => (char) ('0' + (c - '0' + i) % 10));


            string OBD(string s, Func<char, IEnumerable<char>> depSelector) => 
                s.OrderByDependency(depSelector).ToDelimitedString("");

            Assert.Equal("", OBD("", DepSelector1));
            Assert.Equal("01", OBD("1", DepSelector1));
            Assert.Equal("012", OBD("12", DepSelector1));
            Assert.Equal("012", OBD("21", DepSelector1));
            Assert.Equal("0123", OBD("231", DepSelector1));
            
            Assert.Throws<InvalidOperationException>(() => 
                OBD("0", BadDepSelector1).Ignore());
            Assert.Throws<InvalidOperationException>(() => 
                OBD("0", BadDepSelector2).Ignore());
        }
    }
}
