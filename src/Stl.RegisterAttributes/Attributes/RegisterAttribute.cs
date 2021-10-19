using Microsoft.Extensions.DependencyInjection;
using Stl.RegisterAttributes.Internal;

namespace Stl.RegisterAttributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public abstract class RegisterAttribute : Attribute
{
    public string Scope { get; set; } = "";

    public abstract void Register(IServiceCollection services, Type implementationType);

    public static RegisterAttribute[] GetAll(Type implementationType)
        => ServiceInfo.For(implementationType).Attributes;

    public static RegisterAttribute[] GetAll(Type implementationType, Symbol scope)
        => ServiceInfo.For(implementationType, scope).Attributes;
}
