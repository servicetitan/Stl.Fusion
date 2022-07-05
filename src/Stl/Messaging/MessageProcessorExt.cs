namespace Stl.Messaging;

public static class MessageProcessorExt
{
    public static IMessageProcess<TSpecific> Enqueue<TMessage, TSpecific>(
        this IMessageProcessor<TMessage> messageProcessor, 
        TSpecific message, 
        CancellationToken cancellationToken = default)
        where TMessage : class
        where TSpecific : TMessage
        => (IMessageProcess<TSpecific>)messageProcessor.Enqueue((TMessage)message, cancellationToken);
}
