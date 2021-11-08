namespace Stl.Fusion.Operations;

public interface IOperationCompletionListener
{
    Task OnOperationCompleted(IOperation operation);
}
