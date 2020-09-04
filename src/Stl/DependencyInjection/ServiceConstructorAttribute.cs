using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class ServiceConstructorAttribute : ActivatorUtilitiesConstructorAttribute
    { }
}
