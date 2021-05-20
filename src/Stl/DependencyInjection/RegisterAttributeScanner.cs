using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection.Internal;
using Stl.Text;

namespace Stl.DependencyInjection
{
    public record RegisterAttributeScanner
    {
        public IServiceCollection Services { get; }
        public Symbol Scope { get; init; }
        public Func<Type, bool> TypeFilter { get; init; } = _ => true;

        public RegisterAttributeScanner(IServiceCollection services, Symbol scope = default)
        {
            Services = services;
            Scope = scope;
        }

        // SetXxx, ResetXxx

        public RegisterAttributeScanner WithScope(Symbol scope)
            => Scope == scope ? this : this with { Scope =  scope };
        public RegisterAttributeScanner WithTypeFilter(Func<Type, bool> typeFilter)
            => this with { TypeFilter = typeFilter };
        public RegisterAttributeScanner WithTypeFilter(string fullNamePrefix)
            => this with { TypeFilter = t => (t.FullName ?? "").StartsWith(fullNamePrefix) };
        public RegisterAttributeScanner WithTypeFilter(Regex fullNameRegex)
            => this with { TypeFilter = t => fullNameRegex.IsMatch(t.FullName ?? "") };

        // AddService

        public RegisterAttributeScanner Register<TImplementation>()
            => Register(typeof(TImplementation));

        public RegisterAttributeScanner Register(Type implementationType)
        {
            if (!TypeFilter.Invoke(implementationType))
                return this;

            var attrs = RegisterAttribute.GetAll(implementationType, Scope);
            if (attrs.Length == 0)
                throw Errors.NoServiceAttribute(implementationType);
            foreach (var attr in attrs)
                attr.Register(Services, implementationType);
            return this;
        }

        // AddServices

        public RegisterAttributeScanner Register(params Type[] implementationTypes)
        {
            foreach (var implementationType in implementationTypes)
                Register(implementationType);
            return this;
        }

        public RegisterAttributeScanner Register(IEnumerable<Type> implementationTypes)
        {
            foreach (var implementationType in implementationTypes)
                Register(implementationType);
            return this;
        }

        // AddServicesFrom

        public RegisterAttributeScanner RegisterFrom(Assembly assembly)
            => Register(ServiceInfo.ForAll(assembly, Scope), false, true);

        public RegisterAttributeScanner RegisterFrom(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
                RegisterFrom(assembly);
            return this;
        }

        // Private methods

        private RegisterAttributeScanner Register(
            IEnumerable<ServiceInfo> services, bool filterByScope, bool filterByType)
        {
            foreach (var service in services) {
                foreach (var attr in service.Attributes) {
                    var implementationType = service.ImplementationType;
                    if (filterByScope && Scope != attr.Scope)
                        continue;
                    if (filterByType && !TypeFilter.Invoke(implementationType))
                        continue;
                    attr.Register(Services, implementationType);
                }
            }
            return this;
        }
    }
}
