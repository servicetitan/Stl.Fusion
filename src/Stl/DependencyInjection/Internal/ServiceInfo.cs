using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stl.Collections;
using Stl.Concurrency;
using Stl.Text;

namespace Stl.DependencyInjection.Internal
{
    internal readonly struct ServiceInfo
    {
        private static ConcurrentDictionary<Assembly, ServiceInfo[]> ServiceInfoCache { get; } =
            new ConcurrentDictionary<Assembly, ServiceInfo[]>();
        private static ConcurrentDictionary<(Assembly, Symbol, Option<Symbol>), ServiceInfo[]> ScopedServiceInfoCache { get; } =
            new ConcurrentDictionary<(Assembly, Symbol, Option<Symbol>), ServiceInfo[]>();

        public Type ImplementationType { get; }
        public ServiceAttributeBase[] Attributes { get; }

        public ServiceInfo(Type implementationType, ServiceAttributeBase[]? attributes = null)
        {
            ImplementationType = implementationType;
            Attributes = attributes ?? Array.Empty<ServiceAttributeBase>();
        }

        public static ServiceInfo For(Type implementationType)
        {
            using var buffer = ArrayBuffer<ServiceAttributeBase>.Lease();
            foreach (var attr in implementationType.GetCustomAttributes<ServiceAttributeBase>(false))
                buffer.Add(attr);
            if (buffer.Count == 0)
                return new ServiceInfo(implementationType);
            return new ServiceInfo(implementationType, buffer.ToArray());
        }

        public static ServiceInfo For(Type implementationType, Symbol scope, Option<Symbol> fallbackScopeOption = default)
        {
            using var buffer = ArrayBuffer<ServiceAttributeBase>.Lease();
            foreach (var attr in implementationType.GetCustomAttributes<ServiceAttributeBase>(false)) {
                if (attr.Scope == scope.Value)
                    buffer.Add(attr);
            }

            if (buffer.Count != 0)
                return new ServiceInfo(implementationType, buffer.ToArray());
            return fallbackScopeOption.IsSome(out var fallbackScope)
                ? For(implementationType, fallbackScope)
                : new ServiceInfo(implementationType);
        }

        public static ServiceInfo[] ForAll(Assembly assembly)
            => ServiceInfoCache!.GetOrAddChecked(
                assembly, a => a.ExportedTypes
                    .Select(For)
                    .Where(s => s.Attributes.Length != 0)
                    .ToArray())!;

        public static ServiceInfo[] ForAll(Assembly assembly, Symbol scope, Option<Symbol> fallbackScopeOption = default)
            => ScopedServiceInfoCache.GetOrAddChecked(
                (assembly, scope, fallbackScopeOption), key => {
                    var (assembly1, scope1, fallbackScopeOption1) = key;
                    return ForAll(assembly1)
                        .Select(si => For(si.ImplementationType, scope1, fallbackScopeOption1))
                        .Where(s => s.Attributes.Length != 0)
                        .ToArray();
                });
    }
}
