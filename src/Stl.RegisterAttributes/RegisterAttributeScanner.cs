using System.Text.RegularExpressions;
using Stl.RegisterAttributes.Internal;

namespace Stl.RegisterAttributes;

public record RegisterAttributeScanner(
    IServiceCollection Services,
    Symbol Scope = default)
{
    public Func<Type, bool> TypeFilter { get; init; } = _ => true;

    // WithXxx

    public RegisterAttributeScanner WithScope(Symbol scope)
        => Scope == scope ? this : this with { Scope =  scope };
    public RegisterAttributeScanner WithTypeFilter(Func<Type, bool> typeFilter)
        => this with { TypeFilter = typeFilter };
    public RegisterAttributeScanner WithTypeFilter(string fullNamePrefix)
        => this with { TypeFilter = t => (t.FullName ?? "").StartsWith(fullNamePrefix, StringComparison.Ordinal) };
    public RegisterAttributeScanner WithTypeFilter(Regex fullNameRegex)
        => this with { TypeFilter = t => fullNameRegex.IsMatch(t.FullName ?? "") };

    // Register

    public RegisterAttributeScanner Register<TImplementation>()
        => Register(typeof(TImplementation));

    public RegisterAttributeScanner Register(Type implementationType)
    {
        if (!TypeFilter(implementationType))
            return this;
        if (IsDynamicProxy(implementationType))
            return this;

        var attrs = RegisterAttribute.GetAll(implementationType, Scope);
        if (attrs.Length == 0)
            throw Errors.NoServiceAttribute(implementationType);
        foreach (var attr in attrs)
            attr.Register(Services, implementationType);
        return this;
    }

    public RegisterAttributeScanner Register(params Type[] implementationTypes)
        => Register(implementationTypes.AsEnumerable());

    public RegisterAttributeScanner Register(IEnumerable<Type> implementationTypes)
    {
        foreach (var implementationType in implementationTypes)
            Register(implementationType);
        return this;
    }

    // RegisterFrom

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
                if (filterByType && !TypeFilter(implementationType))
                    continue;
                attr.Register(Services, implementationType);
            }
        }
        return this;
    }

    private bool IsDynamicProxy(Type type)
        => (type.Assembly.FullName ?? "").StartsWith("DynamicProxyGenAssembly2", StringComparison.Ordinal);
}
