using System.Reflection;
using Stl.Caching;
using Stl.IO;

namespace Stl.Tests.Caching;

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
        (await cache.Get(1)).Should().Be(0);

        await cache.Set(1, 1);
        (await cache.TryGet(1)).Should().Be(Option.Some<int>(1));
        (await cache.Get(1)).Should().Be(1);

        (await cache.TryGet(2)).Should().Be(Option.None<int>());
        (await cache.Get(2)).Should().Be(0);

        await cache.Remove(1);
        (await cache.TryGet(1)).Should().Be(Option.None<int>());
        (await cache.Get(1)).Should().Be(0);
    }

    private static FilePath GetCacheDir(string id = "", Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        var subdirectory = FilePath.GetHashedName($"{id}_{assembly.FullName}_{assembly.Location}");
        return Path.GetTempPath() & subdirectory;
    }
}
