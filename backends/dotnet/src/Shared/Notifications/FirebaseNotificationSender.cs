using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StackBraid.Shared.Notifications;

public sealed class FirebaseOptions
{
    public const string SectionName = "Notifications:Firebase";

    public string? ProjectId { get; set; }
    public string? AccessToken { get; set; }
}

public interface IFirebaseCloudMessagingTransport
{
    Task SendAsync(IReadOnlyCollection<string> tokens, NotificationMessage notification, CancellationToken cancellationToken = default);
}

public sealed class FirebaseCloudMessagingTransport : IFirebaseCloudMessagingTransport
{
    private readonly HttpClient _httpClient;
    private readonly FirebaseOptions _options;
    private readonly ILogger<FirebaseCloudMessagingTransport> _logger;

    public FirebaseCloudMessagingTransport(HttpClient httpClient, IOptions<FirebaseOptions> options, ILogger<FirebaseCloudMessagingTransport> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(IReadOnlyCollection<string> tokens, NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        if (tokens.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ProjectId) || string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            _logger.LogWarning("Firebase Cloud Messaging transport is not configured; no push request was sent.");
            return;
        }

        foreach (var token in tokens)
        {
            var payload = JsonSerializer.Serialize(new
            {
                message = new
                {
                    token,
                    notification = new { title = notification.Title, body = notification.Body },
                    data = notification.Data ?? new Dictionary<string, string>(),
                },
            });
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://fcm.googleapis.com/v1/projects/{_options.ProjectId}/messages:send")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }
}

public sealed class FirebaseNotificationSender : INotificationSender
{
    private readonly IFirebaseCloudMessagingTransport _transport;
    private readonly IDeviceTokenStore _deviceTokens;
    private readonly FirebaseOptions _options;
    private readonly ILogger<FirebaseNotificationSender> _logger;

    public FirebaseNotificationSender(
        IFirebaseCloudMessagingTransport transport,
        IDeviceTokenStore deviceTokens,
        IOptions<FirebaseOptions> options,
        ILogger<FirebaseNotificationSender> logger)
    {
        _transport = transport;
        _deviceTokens = deviceTokens;
        _options = options.Value;
        _logger = logger;
    }

    public string Channel => "firebase";

    public async Task SendAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ProjectId) || string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            _logger.LogWarning("Firebase Cloud Messaging push is disabled: Firebase credentials are not configured. Email and in-app delivery remain active.");
            return;
        }

        var tokens = await _deviceTokens.GetTokensAsync(notification.UserId, cancellationToken).ConfigureAwait(false);
        await _transport.SendAsync(tokens, notification, cancellationToken).ConfigureAwait(false);
    }
}
