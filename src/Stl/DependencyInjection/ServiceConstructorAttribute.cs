using System;

namespace Stl.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class ServiceConstructorAttribute : Attribute
    {
    }
}
