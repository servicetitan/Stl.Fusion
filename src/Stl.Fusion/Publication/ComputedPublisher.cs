using Stl.Text;

namespace Stl.Fusion.Publication
{
    public interface IComputedPublisher
    {
        IComputedPublication Publish(IClient client, IComputed computed, IRenewalPolicy renewalPolicy);
    }

    public class ComputedPublisher
    {
    }
}
