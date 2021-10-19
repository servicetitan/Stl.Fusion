namespace Stl.Fusion.Operations.Internal;

public static class Errors
{
    public static Exception OperationHasNoCommand()
        => new InvalidOperationException("Operation object has no Command.");
    public static Exception OperationHasNoCommand(string paramName)
        => new ArgumentException("Provided IOperation object has no Command.", paramName);
    public static Exception OperationScopeIsAlreadyClosed()
        => new InvalidOperationException("Operation scope is already closed (committed or rolled back).");
}
