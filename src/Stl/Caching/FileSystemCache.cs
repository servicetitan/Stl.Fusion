using System.Text;
using Newtonsoft.Json;
using Stl.IO;

namespace Stl.Caching;

public abstract class FileSystemCacheBase<TKey, TValue> : AsyncCacheBase<TKey, TValue>
    where TKey : notnull
{
#if !NETSTANDARD2_0
    private const int BufferSize = -1;
#else
    private const int MinAllowedBufferSize = 128;
    private const int BufferSize = MinAllowedBufferSize;
#endif

    public override async ValueTask<Option<TValue>> TryGet(TKey key, CancellationToken cancellationToken = default)
    {
        try {
            await using var fileStreamWrapper = OpenFile(GetFileName(key), false, cancellationToken)
                .ToAsyncDisposableAdapter().ConfigureAwait(false);
            var fileStream = fileStreamWrapper.Target;
            var pairs = Deserialize(await GetText(fileStream, cancellationToken).ConfigureAwait(false));
            return pairs != null && pairs.TryGetValue(key, out var v) ? v : Option<TValue>.None;
        }
        catch (IOException) {
            return default;
        }
    }

    protected override async ValueTask Set(TKey key, Option<TValue> value, CancellationToken cancellationToken = default)
    {
        try {
            // The logic here is more complex than it seems to make sure the update is atomic,
            // i.e. the file is locked for modifications between read & write operations.
            var fileName = GetFileName(key);
            var newText = (string?) null;
            var fileStreamWrapper = OpenFile(fileName, true, cancellationToken)
                .ToAsyncDisposableAdapter().ConfigureAwait(false);
            await using (var _ = fileStreamWrapper) {
                var fileStream = fileStreamWrapper.Target;
                var originalText = await GetText(fileStream, cancellationToken).ConfigureAwait(false);
                var pairs =
                    Deserialize(originalText)
                    ?? new Dictionary<TKey, TValue>();
                pairs.SetOrRemove(key, value);
                newText = Serialize(pairs);
                await SetText(fileStream, newText, cancellationToken).ConfigureAwait(false);
            }
            if (newText == null)
                File.Delete(fileName);
        }
        catch (IOException) {}
    }

    protected abstract FilePath GetFileName(TKey key);

    protected virtual FileStream? OpenFile(FilePath fileName, bool forWrite,
        CancellationToken cancellationToken)
    {
        if (!forWrite)
            return File.Exists(fileName) ? File.OpenRead(fileName) : null;

        var dir = Path.GetDirectoryName(fileName);
        Directory.CreateDirectory(dir!);
        return File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    protected virtual async Task<string?> GetText(FileStream? fileStream, CancellationToken cancellationToken)
    {
        if (fileStream == null)
            return null;
        try {
            fileStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize, true);
            var text = await reader.ReadToEndAsync().ConfigureAwait(false);
            return text.NullIfEmpty();
        }
        catch (IOException) {
            return null;
        }
    }

    protected virtual async ValueTask SetText(FileStream? fileStream, string? text, CancellationToken cancellationToken)
    {
        if (fileStream == null)
            return;
        fileStream.Seek(0, SeekOrigin.Begin);
        await using var writerWrapper = new StreamWriter(fileStream, Encoding.UTF8, BufferSize, true)
            .ToAsyncDisposableAdapter().ConfigureAwait(false);
        var writer = writerWrapper.Target!;
        await writer.WriteAsync(text ?? "").ConfigureAwait(false);
        fileStream.SetLength(fileStream.Position);
    }

    protected virtual Dictionary<TKey, TValue>? Deserialize(string? source)
        => source == null
            ? null
            : JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(source);

    protected virtual string? Serialize(Dictionary<TKey, TValue>? source)
        => source == null || source.Count == 0
            ? null
            : JsonConvert.SerializeObject(source);
}

public class FileSystemCache<TKey, TValue> : FileSystemCacheBase<TKey, TValue>
    where TKey : notnull
{
    protected static readonly string DefaultExtension = ".tmp";
    protected static readonly Func<TKey, FilePath> DefaultKeyToFileNameConverter =
        key => FilePath.GetHashedName(key?.ToString() ?? "0_0");

    public string CacheDirectory { get; }
    public string FileExtension { get; }
    public Func<TKey, FilePath> KeyToFileNameConverter { get; }

    public FileSystemCache(FilePath cacheDirectory, string? extension = null, Func<TKey, FilePath>? keyToFileNameConverter = null)
    {
        CacheDirectory = cacheDirectory;
        FileExtension = extension ?? DefaultExtension;
        KeyToFileNameConverter = keyToFileNameConverter ?? DefaultKeyToFileNameConverter;
    }

    public void Clear()
    {
        if (!Directory.Exists(CacheDirectory))
            return;
        var names = Directory.EnumerateFiles(
            CacheDirectory, "*" + FileExtension,
            SearchOption.TopDirectoryOnly);
        foreach (var name in names)
            File.Delete(name);
    }

    protected override FilePath GetFileName(TKey key)
        => CacheDirectory & (KeyToFileNameConverter(key) + FileExtension);
}
