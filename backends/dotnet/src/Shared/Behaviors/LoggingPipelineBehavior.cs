using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace StackBraid.Shared.Behaviors;

/// <summary>
/// Logs every command and query the dispatcher routes — name, outcome and
/// duration — without knowing anything about a feature's request or
/// response shape. Registered once in <c>Host</c> and applied to every
/// <c>ICommand</c>/<c>IQuery</c> in every feature, so a slow or failing
/// handler shows up in structured logs (correlated by the ambient
/// correlation ID — see <c>Shared/Web/CorrelationIdMiddleware</c>) without
/// each handler logging it by hand.
/// </summary>
public sealed class LoggingPipelineBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<LoggingPipelineBehavior<TMessage, TResponse>> _logger;

    public LoggingPipelineBehavior(ILogger<LoggingPipelineBehavior<TMessage, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TMessage).Name;
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Handling {MessageName}", name);

        try
        {
            var response = await next(message, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Handled {MessageName} in {ElapsedMs}ms", name, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MessageName} failed after {ElapsedMs}ms", name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
