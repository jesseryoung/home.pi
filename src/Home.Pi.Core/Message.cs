using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MediatR;

namespace Home.Pi.Core;

public abstract class Message : INotification
{
    public string Serialize()
    {
        return JsonSerializer.Serialize(this, options: jsonOptions);
    }

    public static Message? Deserialize(string message)
    {
        return JsonSerializer.Deserialize<Message>(message, options: jsonOptions);
    }


    private static readonly JsonSerializerOptions jsonOptions = new();

    static Message()
    {
        // Find Message implementations
        var messageBaseType = typeof(Message);
        var messageTypes = typeof(Message)
            .Assembly
            .DefinedTypes
            .Where(dt => dt.IsAssignableTo(messageBaseType) && !dt.IsGenericTypeDefinition && dt != messageBaseType)
            .ToArray();

        // Configure json poly options
        var polymorphismOptions = new JsonPolymorphismOptions();
        foreach (var requestType in messageTypes)
        {
            polymorphismOptions.DerivedTypes.Add(new(requestType, requestType.FullName!));
        }

        // Configure resolver to use poly options
        var typeResolver = new DefaultJsonTypeInfoResolver();
        typeResolver.Modifiers.Add(t =>
        {
            if (t.Type == messageBaseType)
            {
                t.PolymorphismOptions = polymorphismOptions;
            }
        });
        jsonOptions.TypeInfoResolver = typeResolver;

        jsonOptions.PropertyNameCaseInsensitive = true;
    }

}