namespace SSE;

public class DelegateBackgroundService : BackgroundService
{
    private readonly Func<CancellationToken, Task> f;
    public DelegateBackgroundService(Func<CancellationToken, Task> f) 
        => this.f = f;
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => f(stoppingToken);
}
