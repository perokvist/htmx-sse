using Lib.AspNetCore.ServerSentEvents;
using SSE;
using System.Threading.Channels;
using static SSE.Events;

var builder = WebApplication.CreateBuilder(args);
builder
    .Services
    .AddLogging()
    .AddServerSentEvents()
    .AddSingleton(Channel.CreateUnbounded<Event>(new UnboundedChannelOptions { SingleReader = true }))
    .AddSingleton(sp => sp.GetRequiredService<Channel<Event>>().Writer)
    .AddSingleton(sp => sp.GetRequiredService<Channel<Event>>().Reader)
    .AddSingleton<IHostedService, EventCreator>()
    .AddSingleton<IHostedService, EventPublisher>();

var app = builder.Build();

app
    .UseDefaultFiles()
    .UseStaticFiles()
    .MapServerSentEvents("/sse-test");

app
   .MapGet("/events", (Guid? id) => Results.Extensions.RazorSlice("/Fragments/EventFragment.cshtml", id));
app
   .MapPost("/scheduled", async (HttpRequest req, ChannelWriter<Event> writer) =>
   {
       await writer.WriteAsync(
           new ScheduledEvent(req.Headers.RequestId.Any() ? req.Headers.RequestId.ToString() : Guid.NewGuid().ToString(), 
           $"Hello from cron binding. @ {DateTime.UtcNow} .{(await new StreamReader(req.Body).ReadToEndAsync())}"));
       return Results.Ok();
   });

app.Run();



