namespace Stl.CommandR.Configuration;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class CommandHandlerAttribute : Attribute
{
    public bool IsFilter { get; set; } = false;
    public double Priority { get; set; }

    public CommandHandlerAttribute() { }
    public CommandHandlerAttribute(int priority) { Priority = priority; }
}
