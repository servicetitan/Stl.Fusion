using System.Threading.Tasks;

namespace Stl.Fusion.Operations
{
    public interface IOperationCompletionListener
    {
        Task OnOperationCompletedAsync(IOperation operation);
    }
}
