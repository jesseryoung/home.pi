using System.Text.Json;

namespace Home.Pi.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class MessageAttribute : Attribute
{
    public MessageAttribute(string messageType) => this.MessageType = messageType;

    public string MessageType { get; }
}


public abstract class Message
{
    private const string MessageTypeFieldName = "$messageType";

    public string Serialize()
    {
        // Woah, are you judging me right now? Cut it out alright, like it's not _that_ bad, you know you've seen worse in your own codebase....
        // If you think this is bad, don't look at the Deserialize side of this.

        var type = this.GetType();
        var attribute = Attribute
            .GetCustomAttributes(type)
            .OfType<MessageAttribute>()
            .FirstOrDefault();

        if (attribute == null)
        {
            throw new ArgumentException($"Could not find a {nameof(MessageAttribute)} attribute on {type.FullName}.");
        }

        var alteredMessage = type
            .GetProperties()
            .ToDictionary(k => k.Name, v => v.GetValue(this));

        alteredMessage[MessageTypeFieldName] = attribute.MessageType;

        return JsonSerializer.Serialize(alteredMessage);
    }


    private static readonly Dictionary<string, Type> messageTypeMap = new();
    static Message()
    {

        var messageTypes = typeof(Message)
            .Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(Message)));

        foreach (var messageType in messageTypes)
        {
            var attribute = Attribute
                .GetCustomAttributes(messageType)
                .OfType<MessageAttribute>()
                .FirstOrDefault();
            if (attribute != null)
            {
                messageTypeMap.Add(attribute.MessageType, messageType);
            }
        }

    }

    public static (string? MessageType, Message? Value) Deserialize(string message)
    {
        string? messageType = null;
        if (JsonDocument.Parse(message).RootElement.TryGetProperty(MessageTypeFieldName, out var value))
        {
            messageType = value.GetString();
        }
        if (messageType == null || !messageTypeMap.ContainsKey(messageType))
        {
            return (messageType, null);
        }

        return (messageType, (Message)JsonSerializer.Deserialize(message, messageTypeMap[messageType])!);
    }
}