using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Optional;

namespace Stl.Caching
{
    public abstract class FileSystemCacheBase<TKey, TValue> : CacheBase<TKey, TValue>
        where TKey : notnull
    {
        public override async ValueTask<Option<TValue>> TryGetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            await using var fileStream = OpenFileAsync(GetFileName(key), false, cancellationToken);
            var pairs = Deserialize(await GetTextAsync(fileStream, cancellationToken));
            return pairs?.GetOption(key) ?? default; 
        }

        protected override async ValueTask SetAsync(TKey key, Option<TValue> value, CancellationToken cancellationToken = default)
        {
            // The logic here is more complex than it seems to make sure the update is atomic,
            // i.e. the file is locked for modifications between read & write operations.
            var fileName = GetFileName(key);
            var newText = (string?) null;
            await using (var fileStream = OpenFileAsync(fileName, true, cancellationToken)) {
                var originalText = await GetTextAsync(fileStream, cancellationToken);
                var pairs = 
                    Deserialize(originalText) 
                    ?? new Dictionary<TKey, TValue>();
                pairs.SetOption(key, value);
                newText = Serialize(pairs);
                await SetTextAsync(fileStream, newText, cancellationToken);
            }
            if (newText == null)
                File.Delete(fileName);
        }

        protected abstract string GetFileName(TKey key);

        protected virtual FileStream? OpenFileAsync(string fileName, bool forWrite,
            CancellationToken cancellationToken)
        {
            if (!forWrite)
                return File.Exists(fileName) ? File.OpenRead(fileName) : null;

            var dir = Path.GetDirectoryName(fileName);
            Directory.CreateDirectory(dir);
            return File.OpenWrite(fileName);
        }

        protected virtual async Task<string?> GetTextAsync(FileStream? fileStream, CancellationToken cancellationToken)
        {
            if (fileStream == null)
                return null;
            try {
                fileStream.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(fileStream, Encoding.UTF8);
                var text = await reader.ReadToEndAsync();
                return string.IsNullOrEmpty(text) ? null : text;
            }
            catch (IOException) {
                return null;
            }
        }

        protected virtual async ValueTask SetTextAsync(FileStream? fileStream, string? text, CancellationToken cancellationToken)
        {
            if (fileStream == null)
                return;
            fileStream.Seek(0, SeekOrigin.Begin);
            await using var writer = new StreamWriter(fileStream, Encoding.UTF8);
            await writer.WriteAsync(text ?? "");
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
        protected static readonly Func<TKey, string> DefaultKeyToFileNameConverter = 
            key => FileNameHelper.GetHashedName(key?.ToString());

        public string CacheDirectory { get; }
        public string FileExtension { get; }
        public Func<TKey, string> KeyToFileNameConverter { get; }

        public FileSystemCache(string cacheDirectory, string? extension = null, Func<TKey, string>? keyToFileNameConverter = null)
        {
            CacheDirectory = cacheDirectory;
            FileExtension = extension ?? DefaultExtension;
            KeyToFileNameConverter = keyToFileNameConverter ?? DefaultKeyToFileNameConverter;
        }

        protected override string GetFileName(TKey key) 
            => Path.Join(CacheDirectory, KeyToFileNameConverter(key) + FileExtension);
    }
}
