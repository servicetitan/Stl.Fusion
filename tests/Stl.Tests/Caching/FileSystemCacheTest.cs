using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
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

            (await cache.TryGet(1)).Should().Be(Option.None<int>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get(1));

            await cache.Set(1, 1);
            (await cache.TryGet(1)).Should().Be(Option.Some<int>(1));
            (await cache.Get(1)).Should().Be(1);

            (await cache.TryGet(2)).Should().Be(Option.None<int>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get(2));

            await cache.Remove(1);
            (await cache.TryGet(1)).Should().Be(Option.None<int>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get(1));
        }

        private static FilePath GetCacheDir(string id = "", Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            var subdirectory = FilePath.GetHashedName($"{id}_{assembly.FullName}_{assembly.Location}");
            return Path.GetTempPath() & subdirectory;
        }
    }
}
