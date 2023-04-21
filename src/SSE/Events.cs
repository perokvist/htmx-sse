using Lib.AspNetCore.ServerSentEvents;
using System.Threading.Channels;
using static SSE.Events;

namespace SSE;

public class EventCreator : DelegateBackgroundService
{
    public EventCreator(ChannelWriter<Event> channelWriter) : base(Events.EventCreator(channelWriter))
    {
    }
}

public class EventPublisher : DelegateBackgroundService
{
    public EventPublisher(
        ChannelReader<Event> channelReader,
        IServerSentEventsService service,
        ILogger<EventPublisher> logger) : base(Events.EventPublisher(channelReader, service, logger))
    {
    }
}

public static class Events
{
    public record BackgroundEvent(string EventId, string Message) : Event(EventId, Message);
    public record ScheduledEvent(string EventId, string Message) : Event(EventId, Message);
    public record Event(string EventId, string Message);

    public static ServerSentEvent ToServerEvent(Event @event)  
        => new() { Id = @event.EventId, Type = @event.GetType().Name, Data = new List<string>() { @event.Message } };

    public static Func<CancellationToken, Task> EventCreator(ChannelWriter<Event> channelWriter)
     => async stoppingToken =>
     {
         var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
         while (await timer.WaitForNextTickAsync(stoppingToken))
             await channelWriter.WriteAsync(new BackgroundEvent(Guid.NewGuid().ToString(), $"BackgroundMessage @ {DateTime.UtcNow}"), stoppingToken);
     };

    public static Func<CancellationToken, Task> EventPublisher(ChannelReader<Event> channelReader,
        IServerSentEventsService serverSentEventsService, ILogger logger)
     => async stoppingToken =>
     {
         while (await channelReader.WaitToReadAsync(stoppingToken))
         {
             var e = await channelReader.ReadAsync(stoppingToken);
             var sse = ToServerEvent(e);
             logger.LogInformation("Publishing event {1} {0}", sse.Id, sse.Type);
             await serverSentEventsService.SendEventAsync(sse, stoppingToken);
         }
     };
}
