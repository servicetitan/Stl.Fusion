namespace Stl.Interception;

#pragma warning disable CA1813 // Consider making sealed

[AttributeUsage(AttributeTargets.Method)]
public class ProxyIgnoreAttribute : Attribute;
