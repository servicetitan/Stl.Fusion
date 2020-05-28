using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Stl.Hosting.Plugins
{
    public interface ISectionRegistry
    {
        IEnumerable<Type> Sections { get; }
        ImmutableDictionary<string, T> GetSection<T>();
        ImmutableDictionary<string, T> UpdateSection<T>(
            Func<ImmutableDictionary<string, T>, ImmutableDictionary<string, T>> updater);
    }

    public sealed class SectionRegistry : ISectionRegistry
    {
        private readonly object _lock = new object();
        private ImmutableDictionary<Type, object> _sections = ImmutableDictionary<Type, object>.Empty;
        
        public IEnumerable<Type> Sections => _sections.Keys;
        
        public ImmutableDictionary<string, T> GetSection<T>()
        {
            if (_sections.TryGetValue(typeof(T), out var v))
                return (ImmutableDictionary<string, T>) v;
            return ImmutableDictionary<string, T>.Empty;
        }

        public ImmutableDictionary<string, T> UpdateSection<T>(Func<ImmutableDictionary<string, T>, ImmutableDictionary<string, T>> updater)
        {
            lock (_lock) {
                var section = updater.Invoke(GetSection<T>());
                _sections = section.Count == 0 
                    ? _sections.Remove(typeof(T)) 
                    : _sections.SetItem(typeof(T), section);
                return section;
            }
        }
    }
}
