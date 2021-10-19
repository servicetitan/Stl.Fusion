namespace Stl.CommandR.Configuration;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class CommandHandlerAttribute : Attribute
{
    public bool IsEnabled { get; set; } = true;
    public bool IsFilter { get; set; } = false;
    public double Priority { get; set; }

    public CommandHandlerAttribute() { }
    public CommandHandlerAttribute(bool isEnabled) { IsEnabled = isEnabled; }
    public CommandHandlerAttribute(int priority) { Priority = priority; }
}
