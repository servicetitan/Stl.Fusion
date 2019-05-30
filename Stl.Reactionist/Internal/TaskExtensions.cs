using System.Threading;
using System.Threading.Tasks;

namespace Stl.Reactionist.Internal
{
    public static class TaskExtensions
    {
        // Note that this method won't release the token unless it's cancelled!
        public static Task AsTask(this CancellationToken token, bool throwIfCancelled) => 
            throwIfCancelled 
                ? Task.Delay(-1, token) 
                : Task.Delay(-1, token).ContinueWith(_ => {}, TaskContinuationOptions.OnlyOnCanceled);

        // A safer version of the previous method relying on a secondary token
        public static async Task AsTask(this CancellationToken token, CancellationToken cancellationToken)
        {
            using (var lts = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken)) {
                await lts.Token.AsTask(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
