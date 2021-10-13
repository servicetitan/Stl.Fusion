using Stl.Concurrency;
using Stl.Internal;
using Stl.Reflection;

namespace Stl.Extensibility
{
    public interface IMatchingTypeFinder
    {
        Type? TryFind(Type source, Symbol scope);
    }

    public class MatchingTypeFinder : IMatchingTypeFinder
    {
        private static volatile ImmutableHashSet<Assembly> _assemblies = ImmutableHashSet<Assembly>.Empty;

        public static ImmutableHashSet<Assembly> Assemblies {
            get => _assemblies;
            set => Interlocked.Exchange(ref _assemblies, value);
        }

        private readonly Dictionary<(Type Source, Symbol Scope), Type> _matches;
        private readonly ConcurrentDictionary<(Type Source, Symbol Scope), Type?> _cache = new();

        public MatchingTypeFinder()
            : this(Assemblies) { }
        public MatchingTypeFinder(params Assembly[] assemblies)
            : this((IEnumerable<Assembly>) assemblies) { }
        public MatchingTypeFinder(IEnumerable<Assembly> assemblies)
            : this(assemblies.SelectMany(a => a.GetTypes())) { }
        public MatchingTypeFinder(Dictionary<(Type Source, Symbol Scope), Type> matches)
            => _matches = matches;
        public MatchingTypeFinder(IEnumerable<Type> candidates)
        {
            _matches = new Dictionary<(Type, Symbol), Type>();
            foreach (var type in candidates) {
                var attrs = type.GetCustomAttributes<MatchForAttribute>(false);
                foreach (var attr in attrs)
                    _matches.Add((attr.Source, new Symbol(attr.Scope)), type);
            }
        }

        public virtual Type? TryFind(Type source, Symbol scope)
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

        // AddSearchAssemblies

        public static void AddAssembly(Assembly assembly)
        {
            var spinWait = new SpinWait();
            while (true) {
                var oldSet = Assemblies;
                var newSet = oldSet.Add(assembly);
                if (Interlocked.CompareExchange(ref _assemblies, newSet, oldSet) == oldSet)
                    return;
                spinWait.SpinOnce();
            }
        }

        public static void AddAssemblies(params Assembly[] assemblies)
            => AddAssemblies((IEnumerable<Assembly>) assemblies);

        public static void AddAssemblies(IEnumerable<Assembly> assemblies)
        {
            var spinWait = new SpinWait();
            while (true) {
                var oldSet = Assemblies;
                var newSet = oldSet;
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var searchAssembly in assemblies)
                    newSet = newSet.Add(searchAssembly);
                if (Interlocked.CompareExchange(ref _assemblies, newSet, oldSet) == oldSet)
                    return;
                spinWait.SpinOnce();
            }
        }
    }
}
