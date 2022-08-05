using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;

namespace Home.Pi.Core;

public abstract class Message : INotification
{
    public string Serialize()
    {
        return JsonSerializer.Serialize<Message>(this, options: Message.jsonOptions);
    }

    public static Message? Deserialize(string message)
    {
        return JsonSerializer.Deserialize<Message>(message, options: Message.jsonOptions);
    }

    private static readonly JsonSerializerOptions jsonOptions = new();

    static Message()
    {
        // Find all types that inherit from Message
        var messageBaseType = typeof(Message);
        var messageTypes = messageBaseType
            .Assembly
            .DefinedTypes
            .Where(dt => dt.IsAssignableTo(messageBaseType) && !dt.IsGenericTypeDefinition && dt != messageBaseType)
            .ToArray();


        // Define supported derived types for System.Text.Json to deserialize to
        var typeConfiguration = new JsonPolymorphicTypeConfiguration(messageBaseType);
        foreach (var messageType in messageTypes)
        {
            typeConfiguration = typeConfiguration.WithDerivedType(messageType, messageType.FullName!);
        }

        jsonOptions.PolymorphicTypeConfigurations.Add(typeConfiguration);
    }

}