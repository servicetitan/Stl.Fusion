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
    public abstract class FileSystemCacheBase<TKey, TValue> : ICache<TKey, TValue>
        where TKey : notnull
    {
        public async ValueTask<Option<TValue>> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var pairs = Deserialize(await GetTextAsync(GetFileName(key), cancellationToken));
            return pairs?.GetOption(key) ?? default; 
        }

        public async ValueTask SetAsync(TKey key, Option<TValue> value, CancellationToken cancellationToken = default)
        {
            var fileName = GetFileName(key);
            var pairs = 
                Deserialize(await GetTextAsync(GetFileName(key), cancellationToken)) 
                ?? new Dictionary<TKey, TValue>();
            pairs.SetOption(key, value);
            await SetTextAsync(GetFileName(key), Serialize(pairs), cancellationToken);
        }

        protected abstract string GetFileName(TKey key);

        protected virtual async Task<string?> GetTextAsync(string fileName, CancellationToken cancellationToken)
        {
            if (!File.Exists(fileName))
                return null; // We don't want to throw & catch lots of exceptions
            try {
                return await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationToken);
            }
            catch (IOException) {
                return null;
            }
        }

        protected virtual async ValueTask SetTextAsync(string fileName, string? text, CancellationToken cancellationToken)
        {
            var dir = Path.GetDirectoryName(fileName);
            Directory.CreateDirectory(dir);
            if (text == null)
                File.Delete(fileName);
            else
                await File.WriteAllTextAsync(fileName, text, Encoding.UTF8, cancellationToken);
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
