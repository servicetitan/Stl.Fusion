using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.IO;

namespace Stl.Caching
{
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
                await using var fileStreamWrapper = OpenFile(GetFileName(key), false, cancellationToken).ToAsyncDisposableAdapter();
                var fileStream = fileStreamWrapper.Target;
                var pairs = Deserialize(await GetText(fileStream, cancellationToken).ConfigureAwait(false));
                return pairs?.GetOption(key) ?? default;
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
                await using (var fileStreamWrapper = OpenFile(fileName, true, cancellationToken).ToAsyncDisposableAdapter()) {
                    var fileStream = fileStreamWrapper.Target;
                    var originalText = await GetText(fileStream, cancellationToken).ConfigureAwait(false);
                    var pairs =
                        Deserialize(originalText)
                        ?? new Dictionary<TKey, TValue>();
                    pairs.SetOption(key, value);
                    newText = Serialize(pairs);
                    await SetText(fileStream, newText, cancellationToken).ConfigureAwait(false);
                }
                if (newText == null)
                    File.Delete(fileName);
            }
            catch (IOException) {}
        }

        protected abstract PathString GetFileName(TKey key);

        protected virtual FileStream? OpenFile(PathString fileName, bool forWrite,
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
                return string.IsNullOrEmpty(text) ? null : text;
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
            await using var writerWrapper = new StreamWriter(fileStream, Encoding.UTF8, BufferSize, true).ToAsyncDisposableAdapter();
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
        protected static readonly Func<TKey, PathString> DefaultKeyToFileNameConverter =
            key => PathExt.GetHashedName(key?.ToString() ?? "0_0");

        public string CacheDirectory { get; }
        public string FileExtension { get; }
        public Func<TKey, PathString> KeyToFileNameConverter { get; }

        public FileSystemCache(PathString cacheDirectory, string? extension = null, Func<TKey, PathString>? keyToFileNameConverter = null)
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

        protected override PathString GetFileName(TKey key)
            => CacheDirectory & (KeyToFileNameConverter(key) + FileExtension);
    }
}
