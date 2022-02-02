namespace Home.Pi.Daemon;

public interface IMessageHandler<TMessage>
    where TMessage : Message
{
    Task Handle(TMessage message);
}

public static class MessageHandlerExtensions
{
    public static IServiceCollection AddMessageHandlers(this IServiceCollection services)
    {
        var messageHandlerType = typeof(IMessageHandler<>);

        var handlers = typeof(MessageHandlerExtensions)
            .Assembly
            .GetTypes()
            .Where(t => !t.IsGenericTypeDefinition)
            .SelectMany(t =>
                t.GetInterfaces()
                    .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == messageHandlerType)
                    .Select(x => new
                    {
                        ImplementationType = t,
                        ServiceType = x
                    })
            ).ToList();

        foreach (var handler in handlers)
        {
            services.AddTransient(handler.ServiceType, handler.ImplementationType);
        }

        return services;
    }

    public static async Task InvokeMessageHandler(this IServiceProvider provider, Message message)
    {
        // Oh wouldn't you just *love* to know wtf is going on here....
        var messageHandlerServiceType = typeof(IMessageHandler<>).MakeGenericType(message.GetType());
        var messageHandlerService = provider.GetRequiredService(messageHandlerServiceType);
        var handleMethod = messageHandlerService.GetType().GetMethod(nameof(IMessageHandler<Message>.Handle));
        // Null safety? What's that?
        var responseTask = (Task)handleMethod!.Invoke(messageHandlerService, new[] { message })!;
        await responseTask;
        // Why is this an extention method? Because I like extension methods.
    }
}