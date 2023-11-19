namespace Stl.CommandR.Configuration;

#pragma warning disable CA1813 // Consider making sealed

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class CommandHandlerAttribute : Attribute
{
    public bool IsFilter { get; set; } = false;
#pragma warning disable CA1019
    public double Priority { get; set; }
#pragma warning restore CA1019

    public CommandHandlerAttribute() { }
    public CommandHandlerAttribute(int priority) { Priority = priority; }
}
