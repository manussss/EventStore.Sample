var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<EventStoreClient>(x =>
{
    var settings = EventStoreClientSettings.Create(builder.Configuration.GetConnectionString("EventStore")!);
    settings.ConnectionName = "EventStore";

    return new EventStoreClient(settings);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapPost("api/v1/message", async (
    Message message,
    [FromServices] EventStoreClient client
    ) =>
{
    var @event = new MessageCreated(message.Id, message.Payload);
    var eventData = new EventData(Uuid.NewUuid(), nameof(MessageCreated), JsonSerializer.SerializeToUtf8Bytes(@event).AsMemory());
    var writeResult = await client.AppendToStreamAsync(nameof(Message), StreamState.Any, [eventData]);

    return Results.Created();
});

app.MapGet("api/v1/message/{aggregateId}", async ([FromServices] EventStoreClient client, Guid aggregateId) =>
{
    var streamName = $"{nameof(Message)}-{aggregateId}";

    var streamResult = client.ReadStreamAsync(
        Direction.Forwards,
        nameof(Message),
        StreamPosition.Start
    );

    var events = new List<object>();

    await foreach (var item in streamResult)
    {
        object? deserializedEvent = item.Event.EventType switch
        {
            nameof(MessageCreated) => JsonSerializer.Deserialize<MessageCreated>(item.Event.Data.Span),
            nameof(MessageDeleted) => JsonSerializer.Deserialize<MessageDeleted>(item.Event.Data.Span),
            _ => null
        };

        if (deserializedEvent != null)
        {
            events.Add(deserializedEvent);
        }
    }

    return Results.Ok(events);
});

app.MapDelete("api/v1/message/{id}", async (
    Guid id,
    [FromServices] EventStoreClient client
    ) =>
{
    var @event = new MessageDeleted(id);
    var eventData = new EventData(Uuid.NewUuid(), nameof(MessageDeleted), JsonSerializer.SerializeToUtf8Bytes(@event).AsMemory());
    var writeResult = await client.AppendToStreamAsync(nameof(Message), StreamState.Any, [eventData]);

    return Results.NoContent();
});

app.Run();
