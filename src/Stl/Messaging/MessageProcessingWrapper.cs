namespace Stl.Messaging;

public static class MessageProcessingWrapper
{
    public static async Task Process<TMessage>(
        TMessage message,
        Func<TMessage, CancellationToken, Task> processor,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        try {
            await processor.Invoke(message, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            throw new MessageProcessingException($"{message.GetType().Name} processing failed.", ex) {
                ProcessedMessage = message,
            };
        }
    }

    public static async Task<T> Process<TMessage, T>(
        TMessage message,
        Func<TMessage, CancellationToken, Task<T>> processor,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        try {
            return await processor.Invoke(message, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            throw new MessageProcessingException($"{message.GetType().Name} processing failed.", ex) {
                ProcessedMessage = message,
            };
        }
    }
}
