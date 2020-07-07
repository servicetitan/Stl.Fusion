using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stl.Concurrency;
using Stl.Text;

namespace Stl.Extensibility
{
    public interface IMatchingTypeFinder
    {
        Type? TryFind(Type source, Symbol scope);
    }

    public class MatchingTypeFinder : IMatchingTypeFinder
    {
        private readonly Dictionary<(Type Source, Symbol Scope), Type> _matches;
        private readonly ConcurrentDictionary<(Type Source, Symbol Scope), Type?> _cache = 
            new ConcurrentDictionary<(Type, Symbol), Type?>();

        public MatchingTypeFinder(Dictionary<(Type Source, Symbol Scope), Type> matches) 
            => _matches = matches;
        public MatchingTypeFinder(params Assembly[] assemblies)
            : this(assemblies.SelectMany(a => a.GetTypes())) { }
        public MatchingTypeFinder(IEnumerable<Type> candidates)
        {
            _matches = new Dictionary<(Type, Symbol), Type>();
            foreach (var type in candidates) {
                var attr = type.GetCustomAttribute<MatchForAttribute>(false);
                if (attr == null)
                    continue;
                _matches.Add((attr.Source, attr.Scope), type);
            }
        }

        public Type? TryFind(Type source, Symbol scope)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source)); 
            return _cache.GetOrAddChecked((source, scope), (key, self) => {
                var (source1, scope1) = key;
                var currentType = source1;
                var genericTypeArguments = Array.Empty<Type>();
                while (currentType != null) {
                    var match = self._matches.GetValueOrDefault((currentType, scope1));
                    if (match != null) {
                        if (match.IsGenericTypeDefinition)
                            match = match.MakeGenericType(genericTypeArguments);
                        return match;
                    }

                    if (currentType.IsConstructedGenericType) {
                        genericTypeArguments = currentType.GetGenericArguments();
                        currentType = currentType.GetGenericTypeDefinition();
                    }
                    else
                        currentType = currentType.BaseType;
                }

                return null;
            }, this);
        }
    }
}
