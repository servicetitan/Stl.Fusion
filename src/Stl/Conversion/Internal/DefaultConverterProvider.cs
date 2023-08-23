namespace Stl.Conversion.Internal;

public class DefaultConverterProvider(IServiceProvider services) : ConverterProvider
{
    private readonly ConcurrentDictionary<Type, ISourceConverterProvider> _cache = new();

    protected IServiceProvider Services { get; } = services;

    public override ISourceConverterProvider From(Type sourceType)
        => _cache.GetOrAdd(sourceType, static (sourceType1, self) => {
            var scpType = typeof(ISourceConverterProvider<>).MakeGenericType(sourceType1);
            return (ISourceConverterProvider) self.Services.GetRequiredService(scpType);
        }, this);
}
