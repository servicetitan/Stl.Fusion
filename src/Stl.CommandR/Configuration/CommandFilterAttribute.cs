namespace Stl.CommandR.Configuration;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class CommandFilterAttribute : CommandHandlerAttribute
{
    public CommandFilterAttribute()
        => IsFilter = true;
    public CommandFilterAttribute(int priority) : base(priority)
        => IsFilter = true;
}
