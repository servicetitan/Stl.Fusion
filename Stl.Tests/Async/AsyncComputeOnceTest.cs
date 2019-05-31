using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
    public class AsyncComputeOnceTest : TestBase
    {
        public AsyncComputeOnceTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task OrderByDependencyTestAsync()
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


            async Task<string> OBD(string s, Func<char, IEnumerable<char>> depSelector)
            {
                var result = new List<char>();

                async ValueTask<char> Compute(
                    AsyncComputeOnce<char, char> c1, 
                    char c, CancellationToken ct)
                {
                    foreach (var d in depSelector(c))
                        await c1.GetOrComputeAsync(d).ConfigureAwait(false);
                    result.Add(c);
                    return c;
                }

                var c1 = new AsyncComputeOnce<char, char>(Compute);
                await c1.GetOrComputeAsync(s.ToAsyncEnumerable()).Count();
                return string.Join("", result);
            }

            Assert.Equal("", await OBD("", DepSelector1));
            Assert.Equal("01", await OBD("1", DepSelector1));
            Assert.Equal("012", await OBD("12", DepSelector1));
            Assert.Equal("012", await OBD("21", DepSelector1));
            Assert.Equal("0123", await OBD("231", DepSelector1));
            
            await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                (await OBD("0", BadDepSelector1)).Ignore());
            await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                (await OBD("0", BadDepSelector2)).Ignore());
        }
    }
}
