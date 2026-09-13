using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogInformation(
                "SMTP host not configured — logging email instead of sending. To={To} Subject={Subject}",
                message.To, message.Subject);
            return;
        }

        using var client = new SmtpClient(_options.Host, _options.Port);
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        client.EnableSsl = true;

        using var mail = new MailMessage(_options.FromAddress, message.To, message.Subject, message.HtmlBody)
        {
            IsBodyHtml = true,
        };

        await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
    }
}
