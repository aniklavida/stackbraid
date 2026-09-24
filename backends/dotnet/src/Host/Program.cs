using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackBraid.Database.MySql;
using StackBraid.Database.Postgres;
using StackBraid.Database.SqlServer;
using StackBraid.Features.Identity.Application.Abstractions;
using StackBraid.Features.Identity.Endpoints;
using StackBraid.Host.Documentation;
using StackBraid.Host.HealthChecks;
using StackBraid.Host.Observability;
using StackBraid.Host.Realtime;
using StackBraid.Host.Security;
using StackBraid.Shared;
using StackBraid.Shared.Documents;
using StackBraid.Shared.Jobs;
using StackBraid.Shared.Realtime;
using StackBraid.Shared.Time;
using StackBraid.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
});
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// Configured entirely in code — no Serilog.Settings.Configuration package,
// so Logging:LogLevel in appsettings.json governs the ASP.NET Core
// framework logger, and this governs Serilog's own minimum level.
builder.Host.UseSerilog((_, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

builder.Services.AddStackBraidObservability(builder.Configuration);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// The composition root is where the database choice is made — the only
// place outside Database/<Provider>/ that names one. Database:Provider
// defaults to postgres, the provider this backend's own README documents;
// docs/STRUCTURE.md's isolation rule is enforced for every non-Database,
// non-Host assembly by tests/ArchitectureTests.
var databaseProvider = builder.Configuration["Database:Provider"] ?? "postgres";

builder.Services.AddShared(builder.Configuration);
switch (databaseProvider.ToLowerInvariant())
{
    case "sqlserver":
        builder.Services.AddSqlServerPersistence(
            builder.Configuration.GetConnectionString("SqlServer")
                ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is not configured."));
        break;
    case "mysql":
        builder.Services.AddMySqlPersistence(
            builder.Configuration.GetConnectionString("MySql")
                ?? throw new InvalidOperationException("ConnectionStrings:MySql is not configured."));
        break;
    default:
        builder.Services.AddPostgresPersistence(
            builder.Configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured."));
        break;
}
// Every handler here takes a Scoped repository (itself backed by a Scoped
// DbContext) through its constructor. The library's own default is
// Singleton — its README recommends it for raw throughput — but a
// singleton handler is constructed once and holds whatever Scoped
// repository was active at that moment forever after, so every later
// request reuses the same DbContext concurrently. That is exactly what
// surfaced, twice, as an intermittent
// "a second operation was started on this context instance" exception
// under concurrent load, and is also why `WebApplicationFactory`-based
// tests (which validate the service graph on build, unlike a plain
// `dotnet run`) failed outright the moment one was tried. Handlers hold no
// state of their own beyond what DI gives them, so Scoped is free of that
// problem and correct for this graph.
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("The Jwt configuration section is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SigningKey)),
            // No tolerance past the token's own `exp` — the contract's
            // conformance suite proves an expired token is rejected the
            // instant it expires, not up to a grace window later.
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = JwtProblemDetailsEvents.OnChallengeAsync,
            OnForbidden = JwtProblemDetailsEvents.OnForbiddenAsync,
            // A browser's WebSocket handshake carries no custom headers, so
            // a SignalR client authenticates with `?access_token=` instead
            // of an Authorization header — restricted to the hub paths
            // themselves, never accepted on an ordinary REST request.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/v1/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

// A connection string here is opt-in — SignalR delivers to every client
// connected to *this* process with no backplane at all, correct for one
// instance. Configuring Realtime:BackplaneConnectionString layers a Redis
// (or Valkey — both speak the same wire protocol; which one deploys is not
// decided by this code) backplane underneath via SignalR's own
// AddStackExchangeRedis, so a message published by one instance reaches a
// client connected to another. IRealtimePublisher callers never know which
// mode is active.
var realtimeBackplane = builder.Configuration["Realtime:BackplaneConnectionString"];
var signalRBuilder = builder.Services
    .AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
if (!string.IsNullOrWhiteSpace(realtimeBackplane))
{
    signalRBuilder.AddStackExchangeRedis(realtimeBackplane);
}

builder.Services.AddSingleton<IRealtimePublisher, SignalRRealtimePublisher>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

// No frontend origin is trusted by default — a frontend must be listed
// explicitly (Cors:AllowedOrigins, e.g. a frontend's local dev server)
// before its browser requests are allowed to carry the httpOnly refresh
// cookie. An empty list keeps today's behaviour: no cross-origin access.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (corsAllowedOrigins.Length > 0)
    {
        policy.WithOrigins(corsAllowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }
}));

var app = builder.Build();

if (string.IsNullOrWhiteSpace(builder.Configuration["Notifications:Firebase:ProjectId"]) || string.IsNullOrWhiteSpace(builder.Configuration["Notifications:Firebase:AccessToken"]))
{
    app.Logger.LogWarning("Firebase Cloud Messaging push is disabled: Firebase credentials are not configured. Email and in-app delivery remain active.");
}

app.UseSharedWeb();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapIdentityEndpoints();
app.MapHub<NotificationsHub>("/v1/hubs/notifications");
app.MapHub<JobsHub>("/v1/hubs/jobs");
app.MapDocumentJobEndpoints();
app.MapJobEndpoints();
app.MapOpenApiDocumentation();

switch (databaseProvider.ToLowerInvariant())
{
    case "sqlserver":
        await app.Services.MigrateSqlServerDatabaseAsync();
        await SqlServerSeeder.SeedAsync(app.Services);
        break;
    case "mysql":
        await app.Services.MigrateMySqlDatabaseAsync();
        await MySqlSeeder.SeedAsync(app.Services);
        break;
    default:
        await app.Services.MigratePostgresDatabaseAsync();
        await PostgresSeeder.SeedAsync(app.Services);
        break;
}

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
