using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Optional.Unsafe;
using Stl.Caching;
using Stl.Plugins.Metadata;

namespace Stl.Plugins 
{
    public abstract class CachingPluginEnumeratorBase : IPluginEnumerator
    {
        protected Lazy<ICache<string, string>> Cache { get; }

        protected CachingPluginEnumeratorBase()
        {
            Cache = new Lazy<ICache<string, string>>(CreateCache);
        }

        public PluginSetInfo GetPluginSetInfo() 
            => Task.Run(GetPluginSetInfoAsync).Result;

        protected virtual async Task<PluginSetInfo> GetPluginSetInfoAsync()
        {
            var cache = Cache.Value;
            var cacheKey = GetCacheKey();
            var result = await cache.TryGetAsync(cacheKey);
            if (result.HasValue)
                return Deserialize(result.ValueOrDefault());
            var pluginSetInfo = await CreatePluginSetInfoAsync();
            await cache.SetAsync(cacheKey, Serialize(pluginSetInfo));
            return pluginSetInfo;
        }

        protected virtual JsonSerializerSettings GetJsonSerializerSettings() 
            => new JsonSerializerSettings() {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                Formatting = Formatting.Indented,
            };

        protected virtual string Serialize(PluginSetInfo source) 
            => JsonConvert.SerializeObject(source, GetJsonSerializerSettings());

        protected virtual PluginSetInfo Deserialize(string source) 
            => JsonConvert.DeserializeObject<PluginSetInfo>(source, GetJsonSerializerSettings());

        protected abstract ICache<string, string> CreateCache();
        protected abstract string GetCacheKey();
        protected abstract Task<PluginSetInfo> CreatePluginSetInfoAsync();
    }
}
