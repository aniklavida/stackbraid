namespace StackBraid.Shared.Jobs;

/// <summary>
/// Queues work to run outside the request that triggered it. One
/// implementation ships today, <see cref="InProcessJobScheduler"/> — real
/// asynchronous execution, but in-process and not durable across a restart.
/// A durable, persisted scheduler is a second implementation behind this
/// same interface; no caller changes when it arrives.
/// </summary>
public interface IJobScheduler
{
    void Enqueue(Func<IServiceProvider, CancellationToken, Task> job);
}
