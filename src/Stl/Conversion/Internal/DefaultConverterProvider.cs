using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Conversion.Internal
{
    public class DefaultConverterProvider : ConverterProvider
    {
        private readonly ConcurrentDictionary<Type, ISourceConverterProvider> _cache = new();

        protected IServiceProvider Services { get; }

        public DefaultConverterProvider(IServiceProvider services)
            => Services = services;

        public override ISourceConverterProvider From(Type sourceType)
            => _cache.GetOrAdd(sourceType, (sourceType1, self) => {
                var scpType = typeof(ISourceConverterProvider<>).MakeGenericType(sourceType1);
                return (ISourceConverterProvider) self.Services.GetRequiredService(scpType);
            }, this);
    }
}
