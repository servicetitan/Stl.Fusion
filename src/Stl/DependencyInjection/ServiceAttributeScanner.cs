using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection.Internal;
using Stl.Internal;
using Stl.Text;

namespace Stl.DependencyInjection
{
    public record ServiceAttributeScanner
    {
        public IServiceCollection Services { get; }
        public Symbol Scope { get; set; } = Symbol.Empty;
        public Option<Symbol> FallbackScopeOption { get; set; }
        public Func<Type, bool> TypeFilter { get; set; } = _ => true;

        public ServiceAttributeScanner(IServiceCollection services)
            => Services = services;

        public IServiceCollection BackToServices() => Services;

        // SetXxx, ResetXxx

        public ServiceAttributeScanner WithScope(Symbol scope)
            => this with { Scope =  scope };
        public ServiceAttributeScanner WithScope(Symbol scope, Symbol fallbackScope)
            => this with { Scope = scope, FallbackScopeOption = fallbackScope };
        public ServiceAttributeScanner WithTypeFilter(Func<Type, bool> typeFilter)
            => this with { TypeFilter = typeFilter };
        public ServiceAttributeScanner WithTypeFilter(string fullNamePrefix)
            => this with { TypeFilter = t => (t.FullName ?? "").StartsWith(fullNamePrefix) };
        public ServiceAttributeScanner WithTypeFilter(Regex fullNameRegex)
            => this with { TypeFilter = t => fullNameRegex.IsMatch(t.FullName ?? "") };

        // AddService

        public ServiceAttributeScanner AddService<TImplementation>(bool ignoreTypeFilter = false)
            => AddService(typeof(TImplementation), ignoreTypeFilter);

        public ServiceAttributeScanner AddService(Type implementationType, bool ignoreTypeFilter = false)
        {
            if (!ignoreTypeFilter && !TypeFilter.Invoke(implementationType))
                return this;

            var attrs = ServiceAttributeBase.GetAll(implementationType, Scope);
            if (attrs.Length == 0) {
                if (!FallbackScopeOption.IsSome(out var fallbackScope))
                    throw Errors.NoServiceAttribute(implementationType);
                attrs = ServiceAttributeBase.GetAll(implementationType, fallbackScope);
                if (attrs.Length == 0)
                    throw Errors.NoServiceAttribute(implementationType);
            }
            foreach (var attr in attrs)
                attr.Register(Services, implementationType);
            return this;
        }

        // AddServices

        public ServiceAttributeScanner AddServices(params Type[] implementationTypes)
        {
            foreach (var implementationType in implementationTypes)
                AddService(implementationType);
            return this;
        }

        public ServiceAttributeScanner AddServices(IEnumerable<Type> implementationTypes)
        {
            foreach (var implementationType in implementationTypes)
                AddService(implementationType);
            return this;
        }

        // AddServicesFrom

        public ServiceAttributeScanner AddServicesFrom(Assembly assembly)
            => AddServices(ServiceInfo.ForAll(assembly, Scope, FallbackScopeOption));

        public ServiceAttributeScanner AddServicesFrom(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
                AddServicesFrom(assembly);
            return this;
        }

        // Private methods

        private ServiceAttributeScanner AddServices(
            IEnumerable<ServiceInfo> scopeBasedCandidates)
        {
            foreach (var service in scopeBasedCandidates) {
                foreach (var attr in service.Attributes) {
                    var implementationType = service.ImplementationType;
                    if (TypeFilter.Invoke(implementationType))
                        attr.Register(Services, implementationType);
                }
            }
            return this;
        }
    }
}
