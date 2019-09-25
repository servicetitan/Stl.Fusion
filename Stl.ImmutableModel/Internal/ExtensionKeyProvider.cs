using System;
using System.Collections.Concurrent;
using Stl.Reflection;

namespace Stl.ImmutableModel.Internal
{
    public class ExtensionKeyProvider
    {
        public static readonly string ExtensionPrefix = "@Ext_";
        private static object Lock = new object();
        private static readonly ConcurrentDictionary<Type, Symbol> Cache =
            new ConcurrentDictionary<Type, Symbol>();
        private static readonly ConcurrentDictionary<Symbol, Type> InvertedCache =
            new ConcurrentDictionary<Symbol, Type>();

        public static Symbol GetLocalKey(Type type)
        {
            if (Cache.TryGetValue(type, out var localKey))
                return localKey;
            localKey = new Symbol(ExtensionPrefix + type.ToMethodName(true, true));
            lock (Lock) {
                if (!Cache.TryAdd(type, localKey))
                    return localKey;
                if (!InvertedCache.TryAdd(localKey, type)) {
                    var otherType = InvertedCache[localKey];
                    throw Errors.MoreThanOneTypeMapsToTheSameLocalKey(type, otherType, localKey);
                }
                return localKey;
            }
        }
    }
}
