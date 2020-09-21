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
    public class ServiceAttributeBasedBuilder
    {
        public IServiceCollection Services { get; }
        public Symbol Scope { get; private set; } = Symbol.Empty;
        public Option<Symbol> FallbackScopeOption { get; private set; }
        public Func<Type, bool> TypeFilter { get; private set; } = _ => true;

        public ServiceAttributeBasedBuilder(IServiceCollection services)
            => Services = services;

        // SetXxx, ResetXxx

        public ServiceAttributeBasedBuilder SetScope(Symbol scope)
        {
            Scope = scope;
            return this;
        }

        public ServiceAttributeBasedBuilder SetScope(Symbol scope, Symbol fallbackScope)
        {
            Scope = scope;
            FallbackScopeOption = fallbackScope;
            return this;
        }

        public ServiceAttributeBasedBuilder SetTypeFilter(Func<Type, bool> typeFilter)
        {
            TypeFilter = typeFilter;
            return this;
        }

        public ServiceAttributeBasedBuilder SetTypeFilter(string fullNamePrefix)
        {
            TypeFilter = t => (t.FullName ?? "").StartsWith(fullNamePrefix);
            return this;
        }

        public ServiceAttributeBasedBuilder SetTypeFilter(Regex fullNameRegex)
        {
            TypeFilter = t => fullNameRegex.IsMatch(t.FullName ?? "");
            return this;
        }

        public ServiceAttributeBasedBuilder ResetScope()
        {
            Scope = Symbol.Empty;
            FallbackScopeOption = Option<Symbol>.None;
            return this;
        }

        public ServiceAttributeBasedBuilder ResetTypeFilter()
        {
            TypeFilter = _ => true;
            return this;
        }

        // AddService

        public ServiceAttributeBasedBuilder AddService<TImplementation>(bool ignoreTypeFilter = false)
            => AddService(typeof(TImplementation), ignoreTypeFilter);

        public ServiceAttributeBasedBuilder AddService(Type implementationType, bool ignoreTypeFilter = false)
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

        public ServiceAttributeBasedBuilder AddServices(params Type[] implementationTypes)
        {
            foreach (var implementationType in implementationTypes)
                AddService(implementationType);
            return this;
        }

        public ServiceAttributeBasedBuilder AddServices(IEnumerable<Type> implementationTypes)
        {
            foreach (var implementationType in implementationTypes)
                AddService(implementationType);
            return this;
        }

        // AddServicesFrom

        public ServiceAttributeBasedBuilder AddServicesFrom(Assembly assembly)
            => AddServices(ServiceInfo.ForAll(assembly, Scope, FallbackScopeOption));

        public ServiceAttributeBasedBuilder AddServicesFrom(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
                AddServicesFrom(assembly);
            return this;
        }

        // Private methods

        private ServiceAttributeBasedBuilder AddServices(
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
