using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Specialized;
using Optional;
using Stl.Caching;
using Stl.IO;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Caching
{
    public class FileSystemCacheTest : TestBase
    {
        public FileSystemCacheTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var cacheDir = GetCacheDir();
            if (Directory.Exists(cacheDir))
                Directory.Delete(cacheDir, true);
            var cache = new FileSystemCache<int, int>(cacheDir);

            (await cache.TryGetAsync(1)).Should().Be(Option.None<int>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetAsync(1));
            
            await cache.SetAsync(1, 1);
            (await cache.TryGetAsync(1)).Should().Be(Option.Some<int>(1));
            (await cache.GetAsync(1)).Should().Be(1);
            
            (await cache.TryGetAsync(2)).Should().Be(Option.None<int>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetAsync(2));

            await cache.InvalidateAsync(1);
            (await cache.TryGetAsync(1)).Should().Be(Option.None<int>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetAsync(1));
        }

        private static string GetCacheDir(string id = "", Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            var subdirectory = PathEx.GetHashedName($"{id}_{assembly.FullName}_{assembly.Location}");
            return Path.Combine(Path.GetTempPath(), subdirectory);
        }
    }
}
