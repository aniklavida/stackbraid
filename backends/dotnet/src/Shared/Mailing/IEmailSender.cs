namespace StackBraid.Shared.Mailing;

public sealed record EmailMessage(string To, string Subject, string HtmlBody);

/// <summary>
/// Sends one templated email. One implementation ships today,
/// <see cref="SmtpEmailSender"/> — a real SMTP client when a host is
/// configured, and an honest log line instead of a silent no-op when it
/// isn't (a fresh clone has no mail relay configured out of the box).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
