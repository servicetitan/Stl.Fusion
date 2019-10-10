using System;
using System.Collections.Concurrent;
using Stl.Reflection;

namespace Stl.ImmutableModel.Internal
{
    public class ExtensionKeyCache
    {
        private static object Lock = new object();
        private static readonly ConcurrentDictionary<Type, Symbol> KeyCache =
            new ConcurrentDictionary<Type, Symbol>();
        private static readonly ConcurrentDictionary<Symbol, Type> InvertedKeyCache =
            new ConcurrentDictionary<Symbol, Type>();

        public static Symbol Get(Type type)
        {
            if (KeyCache.TryGetValue(type, out var key))
                return key;
            key = new Symbol(ExtendableNodeEx.PropertyKeyPrefix + type.ToMethodName(true, true));
            lock (Lock) {
                if (!KeyCache.TryAdd(type, key))
                    return key;
                if (!InvertedKeyCache.TryAdd(key, type)) {
                    var otherType = InvertedKeyCache[key];
                    throw Errors.MoreThanOneTypeMapsToTheSameLocalKey(type, otherType, key);
                }
                return key;
            }
        }
    }
}
