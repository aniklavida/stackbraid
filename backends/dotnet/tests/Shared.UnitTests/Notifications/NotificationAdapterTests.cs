using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using StackBraid.Shared.Jobs;
using StackBraid.Shared.Mailing;
using StackBraid.Shared.Notifications;

namespace StackBraid.Shared.UnitTests.Notifications;

public sealed class NotificationAdapterTests
{
    [Fact]
    public async Task Queued_notification_dispatches_to_all_senders_and_stores_in_app_record()
    {
        var transport = new RecordingFirebaseTransport();
        var deviceTokens = new InMemoryDeviceTokenStore();
        var userId = Guid.NewGuid();
        await deviceTokens.RegisterAsync(userId, "device-token", "ios", "1.0");
        var firebase = new FirebaseNotificationSender(
            transport,
            deviceTokens,
            Options.Create(new FirebaseOptions { ProjectId = "project", AccessToken = "token" }),
            NullLogger<FirebaseNotificationSender>.Instance);
        var emailTransport = new RecordingEmailSender();
        var email = new EmailNotificationSender(emailTransport);
        var inAppStore = new InMemoryNotificationStore();
        var inApp = new InAppNotificationSender(inAppStore);
        var services = new ServiceCollection()
            .AddSingleton<INotificationSender>(firebase)
            .AddSingleton<INotificationSender>(email)
            .AddSingleton<INotificationSender>(inApp)
            .AddSingleton<NotificationDispatcher>()
            .BuildServiceProvider();
        var scheduler = new CapturingJobScheduler();
        var job = new QueuedNotificationJob(scheduler);
        var notification = new NotificationMessage(userId, "person@example.com", "Job complete", "Your export is ready.");

        job.Enqueue(notification);
        await scheduler.RunAsync(services);

        transport.Notifications.ShouldHaveSingleItem().ShouldBe(notification);
        emailTransport.Messages.ShouldHaveSingleItem().To.ShouldBe("person@example.com");
        (await inAppStore.ListAsync(userId, 1, 20)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Missing_firebase_configuration_logs_disablement_and_keeps_email_and_in_app_active()
    {
        var store = new InMemoryNotificationStore();
        var emailTransport = new RecordingEmailSender();
        var transport = new RecordingFirebaseTransport();
        var logger = new ListLogger<FirebaseNotificationSender>();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new ListLoggerProvider(logger)));
        var firebase = new FirebaseNotificationSender(
            transport,
            new InMemoryDeviceTokenStore(),
            Options.Create(new FirebaseOptions()),
            loggerFactory.CreateLogger<FirebaseNotificationSender>());
        var notification = new NotificationMessage(Guid.NewGuid(), "person@example.com", "Title", "Body");
        var dispatcher = new NotificationDispatcher([firebase, new EmailNotificationSender(emailTransport), new InAppNotificationSender(store)]);

        await dispatcher.DispatchAsync(notification);

        emailTransport.Messages.ShouldHaveSingleItem();
        (await store.ListAsync(notification.UserId, 1, 20)).ShouldHaveSingleItem();
        logger.Messages.ShouldContain(message => message.Contains("push is disabled", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingFirebaseTransport : IFirebaseCloudMessagingTransport
    {
        public List<NotificationMessage> Notifications { get; } = [];

        public Task SendAsync(IReadOnlyCollection<string> tokens, NotificationMessage notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingJobScheduler : IJobScheduler
    {
        private Func<IServiceProvider, CancellationToken, Task>? _job;

        public void Enqueue(Func<IServiceProvider, CancellationToken, Task> job)
        {
            _job = job;
        }

        public Task RunAsync(IServiceProvider services)
        {
            return _job?.Invoke(services, CancellationToken.None) ?? Task.CompletedTask;
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class ListLoggerProvider(ListLogger<FirebaseNotificationSender> logger) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => logger;
        public void Dispose() { }
    }
}
