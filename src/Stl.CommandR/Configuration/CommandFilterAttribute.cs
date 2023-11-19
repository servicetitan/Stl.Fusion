namespace Stl.CommandR.Configuration;

#pragma warning disable CA1813 // Consider making sealed

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class CommandFilterAttribute : CommandHandlerAttribute
{
    public CommandFilterAttribute()
        => IsFilter = true;
    public CommandFilterAttribute(int priority) : base(priority)
        => IsFilter = true;
}
