namespace EventStore.Sample.API.Domain;

public class Message
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Payload { get; set; } = string.Empty;
}
