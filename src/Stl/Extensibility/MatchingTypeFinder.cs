using Stl.Concurrency;
using Stl.Internal;

namespace Stl.Extensibility;

public interface IMatchingTypeFinder
{
    Type? TryFind(Type source, Symbol scope);
}

public class MatchingTypeFinder : IMatchingTypeFinder
{
    public record Options
    {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public IEnumerable<Assembly> ScannedAssemblies { get; init; } = Array.Empty<Assembly>();
        public IEnumerable<Type> ScannedTypes { get; init; } = Array.Empty<Type>();
    }

    private static volatile ImmutableHashSet<Assembly> _scannedAssemblies =
        ImmutableHashSet<Assembly>.Empty
            .Add(typeof(MatchingTypeFinder).Assembly);

    public static ImmutableHashSet<Assembly> ScannedAssemblies {
        get => _scannedAssemblies;
        set => Interlocked.Exchange(ref _scannedAssemblies, value);
    }

    private readonly Dictionary<(Type Source, Symbol Scope), Type> _matches;
    private readonly ConcurrentDictionary<(Type Source, Symbol Scope), Type?> _cache = new();

    public MatchingTypeFinder() : this(new()) { }
    public MatchingTypeFinder(Options options)
    {
        var types = ScannedAssemblies
            .Concat(options.ScannedAssemblies)
            .SelectMany(a => a.GetTypes())
            .Concat(options.ScannedTypes);
        _matches = new Dictionary<(Type, Symbol), Type>();
        foreach (var type in types) {
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

    public static void AddScannedAssembly(Assembly assembly)
    {
        var spinWait = new SpinWait();
        while (true) {
            var oldSet = ScannedAssemblies;
            var newSet = oldSet.Add(assembly);
            if (Interlocked.CompareExchange(ref _scannedAssemblies, newSet, oldSet) == oldSet)
                return;
            spinWait.SpinOnce();
        }
    }

    public static void AddScannedAssemblies(params Assembly[] assemblies)
        => AddScannedAssemblies((IEnumerable<Assembly>) assemblies);

    public static void AddScannedAssemblies(IEnumerable<Assembly> assemblies)
    {
        var spinWait = new SpinWait();
        while (true) {
            var oldSet = ScannedAssemblies;
            var newSet = oldSet;
            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var searchAssembly in assemblies)
                newSet = newSet.Add(searchAssembly);
            if (Interlocked.CompareExchange(ref _scannedAssemblies, newSet, oldSet) == oldSet)
                return;
            spinWait.SpinOnce();
        }
    }
}
