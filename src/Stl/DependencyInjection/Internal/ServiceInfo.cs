using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Stl.Concurrency;

namespace Stl.DependencyInjection.Internal
{
    internal class ServiceInfo
    {
        private static ConcurrentDictionary<Assembly, ServiceInfo[]> ServiceDefCache { get; } =
            new ConcurrentDictionary<Assembly, ServiceInfo[]>();
        private static ConcurrentDictionary<(Assembly, string), ServiceInfo[]> ScopedServiceDefCache { get; } =
            new ConcurrentDictionary<(Assembly, string), ServiceInfo[]>();

        public Type ImplementationType { get; }
        public ServiceAttributeBase[] Attributes { get; }

        public ServiceInfo(Type implementationType, ServiceAttributeBase[] attributes)
        {
            ImplementationType = implementationType;
            Attributes = attributes;
        }

        public static ServiceInfo? For(Type implementationType)
        {
            var attrs = implementationType.GetCustomAttributes<ServiceAttributeBase>(false);
            if (attrs == null)
                return null;
            var aAttrs = attrs.ToArray();
            if (aAttrs.Length == 0)
                return null;
            return new ServiceInfo(implementationType, aAttrs);
        }

        public static ServiceInfo? For(Type implementationType, string scope)
        {
            var attrs = implementationType.GetCustomAttributes<ServiceAttributeBase>(false);
            if (attrs == null)
                return null;
            var aAttrs = attrs.Where(a => a.Scope == scope).ToArray();
            if (aAttrs.Length == 0)
                return null;
            return new ServiceInfo(implementationType, aAttrs);
        }

        public static ServiceInfo[] ForAll(Assembly assembly)
            => ServiceDefCache!.GetOrAddChecked(
                assembly, a => a.ExportedTypes
                    .Select(For)
                    .Where(d => d != null)
                    .ToArray())!;

        public static ServiceInfo[] ForAll(Assembly assembly, string scope)
            => ScopedServiceDefCache.GetOrAddChecked(
                (assembly, scope), key => {
                    var (assembly1, scope1) = key;
                    return ForAll(assembly1)
                        .Where(d => d.Attributes.Any(a => a.Scope == scope1))
                        .Select(d => new ServiceInfo(
                            d.ImplementationType,
                            d.Attributes.Where(a => a.Scope == scope1).ToArray()))
                        .ToArray();
                });
    }
}
