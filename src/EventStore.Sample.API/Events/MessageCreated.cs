namespace EventStore.Sample.API.Events;

public class MessageCreated(Guid id, string payload)
{
    public Guid Id { get; set; } = id;
    public string Payload { get; set; } = payload;
}
