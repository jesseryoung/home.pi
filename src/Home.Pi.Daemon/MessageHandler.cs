using MediatR;

namespace Home.Pi.Daemon;

public abstract class MessageHandler<TMessage> : INotificationHandler<TMessage>
    where TMessage : Message
{
    public async Task Handle(TMessage notification, CancellationToken cancellationToken)
    {
        await this.HandleMessage(notification, cancellationToken);
    }

    public abstract Task HandleMessage(TMessage notification, CancellationToken cancellationToken);
}
