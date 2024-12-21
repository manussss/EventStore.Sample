namespace EventStore.Sample.API.Events;

public class MessageDeleted(Guid Id)
{
    public Guid Id { get; set; } = Id;
}
