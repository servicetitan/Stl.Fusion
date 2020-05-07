using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Publish.Messages;

namespace Stl.Fusion.Publish
{
    public interface IPublicationHandler
    {
        Task OnStateChangedAsync(
            IPublication publication, PublicationState previousState, Message? message, 
            CancellationToken cancellationToken);
    }

    public abstract class PublicationHandlerBase : IPublicationHandler
    {
        public abstract Task OnStateChangedAsync(
            IPublication publication, PublicationState previousState, Message? message, 
            CancellationToken cancellationToken);
    }

    public class PublicationHandler : PublicationHandlerBase
    {
        private readonly Func<IPublication, PublicationState, Message?, CancellationToken, Task> _stateChanged;

        public PublicationHandler(Func<IPublication, PublicationState, Message?, CancellationToken, Task> stateChanged) 
            => _stateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));

        public override Task OnStateChangedAsync(
            IPublication publication, PublicationState previousState, Message? message, 
            CancellationToken cancellationToken) 
            => _stateChanged.Invoke(publication, previousState, message, cancellationToken);
    }
}
