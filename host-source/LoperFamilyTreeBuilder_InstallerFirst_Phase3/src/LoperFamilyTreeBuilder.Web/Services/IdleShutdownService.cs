namespace LoperFamilyTreeBuilder.Web.Services;

public sealed class IdleShutdownService(
    ActiveCircuitTracker tracker,
    IHostApplicationLifetime lifetime,
    ILogger<IdleShutdownService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ShutdownGracePeriod = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            if (!tracker.HasSeenCircuit || tracker.ActiveCircuits > 0)
                continue;

            var idleFor = DateTimeOffset.UtcNow - tracker.LastCircuitClosedUtc;
            if (idleFor < ShutdownGracePeriod)
                continue;

            logger.LogInformation(
                "No active browser sessions remain. Shutting down after {GracePeriod}.",
                ShutdownGracePeriod);

            lifetime.StopApplication();
            return;
        }
    }
}
