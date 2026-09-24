using StackBraid.Shared.Mailing;

namespace StackBraid.Shared.Notifications;

public sealed class EmailNotificationSender : INotificationSender
{
    private readonly IEmailSender _emailSender;

    public EmailNotificationSender(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public string Channel => "email";

    public Task SendAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        var html = $"<h1>{System.Net.WebUtility.HtmlEncode(notification.Title)}</h1><p>{System.Net.WebUtility.HtmlEncode(notification.Body)}</p>";
        return _emailSender.SendAsync(new EmailMessage(notification.Email, notification.Title, html), cancellationToken);
    }
}
