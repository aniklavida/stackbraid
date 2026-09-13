using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StackBraid.Shared.Jobs;

public sealed class InProcessJobScheduler : IJobScheduler
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _channel;

    public InProcessJobScheduler(Channel<Func<IServiceProvider, CancellationToken, Task>> channel)
    {
        _channel = channel;
    }

    public void Enqueue(Func<IServiceProvider, CancellationToken, Task> job) =>
        _channel.Writer.TryWrite(job);
}

/// <summary>Runs each queued job with its own DI scope, one at a time, logging failures instead of crashing the process.</summary>
public sealed class JobQueueHostedService : BackgroundService
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobQueueHostedService> _logger;

    public JobQueueHostedService(
        Channel<Func<IServiceProvider, CancellationToken, Task>> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<JobQueueHostedService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            try
            {
                await job(scope.ServiceProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job failed.");
            }
        }
    }
}
