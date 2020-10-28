using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Caching;
using Stl.Collections;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Caching
{
    public class ComputingCacheTest : TestBase
    {
        public ComputingCacheTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task ComputingCache_OrderByDependencyTest()
        {
            await OrderByDependencyTestAsync(computer => new ComputingCache<char, char>(
                new MemoizingCache<char, char>(),
                computer));
        }

        [Fact]
        public async Task FastComputingCache_OrderByDependencyTest()
        {
            await OrderByDependencyTestAsync(computer => new FastComputingCache<char, char>(computer));
        }

        private async Task OrderByDependencyTestAsync(
            Func<
                Func<char, CancellationToken, ValueTask<char>>,
                IAsyncKeyResolver<char, char>> cacheFactory)
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

                IAsyncKeyResolver<char, char>? cache = null;

                async ValueTask<char> Compute(char c, CancellationToken ct)
                {
                    if (cache == null)
                        throw new NullReferenceException();
                    foreach (var d in depSelector(c))
                        // ReSharper disable once AccessToModifiedClosure
                        await cache.GetAsync(d).ConfigureAwait(false);
                    result.Add(c);
                    return c;
                }

                cache = cacheFactory(Compute);
                await cache.GetManyAsync(s.ToAsyncEnumerable()).CountAsync();
                return result.ToDelimitedString("");
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
