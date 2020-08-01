using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stl.Concurrency;
using Stl.Internal;
using Stl.Reflection;
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
                var attrs = type.GetCustomAttributes<MatchForAttribute>(false);
                if (attrs == null)
                    continue;
                foreach (var attr in attrs)
                    _matches.Add((attr.Source, new Symbol(attr.Scope)), type);
            }
        }

        public Type? TryFind(Type source, Symbol scope)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return _cache.GetOrAddChecked((source, scope), (key, self) => {
                var (source1, scope1) = key;
                var baseTypes = source1.GetAllBaseTypes(true, true);
                foreach (var cType in baseTypes) {
                    var match = self._matches.GetValueOrDefault((cType, scope1));
                    if (match != null) {
                        if (match.IsGenericTypeDefinition)
                            throw Errors.GenericMatchForConcreteType(cType, match);
                        return match;
                    }
                    if (cType.IsConstructedGenericType) {
                        var gType = cType.GetGenericTypeDefinition();
                        var gTypeArgs = cType.GetGenericArguments();
                        match = self._matches.GetValueOrDefault((gType, scope1));
                        if (match != null) {
                            if (!match.IsGenericTypeDefinition)
                                throw Errors.ConcreteMatchForGenericType(gType, match);
                            match = match.MakeGenericType(gTypeArgs);
                            return match;
                        }
                    }
                }
                return null;
            }, this);
        }
    }
}
