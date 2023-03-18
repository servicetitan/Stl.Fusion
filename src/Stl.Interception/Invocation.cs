namespace Stl.Interception;

public readonly struct Invocation
{
    public object Subject { get; }
    public MethodInfo MethodInfo { get; }
    public ArgumentList Arguments { get; }
    public Delegate Delegate { get; }

    public Invocation(object subject, MethodInfo methodInfo, Delegate @delegate, ArgumentList arguments)
    {
        Subject = subject;
        MethodInfo = methodInfo;
        Delegate = @delegate;
        Arguments = arguments;
    }
}
