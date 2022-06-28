namespace Stl.Fusion.Operations;

public interface IOperationCompletionListener
{
    bool IsReady();
    Task OnOperationCompleted(IOperation operation);
}
