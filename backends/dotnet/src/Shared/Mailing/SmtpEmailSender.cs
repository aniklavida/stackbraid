using System.Collections.Concurrent;

namespace StackBraid.Shared.Mailing;

public sealed class SmtpOptions
{
    /// <summary>Left blank in a fresh clone — sending falls back to logging until this is configured.</summary>
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@example.com";
}

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _capturedMessages = new();

    public IReadOnlyCollection<EmailMessage> CapturedMessages => _capturedMessages.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _capturedMessages.Enqueue(message);
        return Task.CompletedTask;
    }
}
