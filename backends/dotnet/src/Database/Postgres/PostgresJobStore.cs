using System.Data;
using Npgsql;
using NpgsqlTypes;
using StackBraid.Shared.Jobs;

namespace StackBraid.Database.Postgres;

/// <summary>
/// The persisted <see cref="IJobStore"/>: a queued job is a row in
/// <c>shared_jobs</c>, so it is still there when the API process restarts.
/// This is the only place the job store knows it is Postgres — the port it
/// implements lives in <c>Shared</c> and names no provider.
/// </summary>
public sealed class PostgresJobStore : IJobStore
{
    private const string SelectColumns =
        "id, type, payload, owner_id, state, attempts, max_attempts, last_error, created_at, updated_at, next_attempt_at";

    private readonly NpgsqlDataSource _dataSource;

    public PostgresJobStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    /// <summary>Creates the job table and its indexes if they do not exist — idempotent, safe to call on every startup.</summary>
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS shared_jobs (
                id uuid PRIMARY KEY,
                type text NOT NULL,
                payload jsonb NOT NULL DEFAULT '{}'::jsonb,
                owner_id text NULL,
                state text NOT NULL,
                attempts integer NOT NULL DEFAULT 0,
                max_attempts integer NOT NULL DEFAULT 3,
                last_error text NULL,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL,
                next_attempt_at timestamptz NULL
            );
            CREATE INDEX IF NOT EXISTS ix_shared_jobs_state_next_attempt_at ON shared_jobs (state, next_attempt_at);
            CREATE INDEX IF NOT EXISTS ix_shared_jobs_owner_id ON shared_jobs (owner_id);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(JobRecord job, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO shared_jobs
                (id, type, payload, owner_id, state, attempts, max_attempts, last_error, created_at, updated_at, next_attempt_at)
            VALUES
                (@id, @type, @payload, @owner_id, @state, @attempts, @max_attempts, @last_error, @created_at, @updated_at, @next_attempt_at)
            """;

        await using var command = _dataSource.CreateCommand(sql);
        AddRecordParameters(command, job);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<JobRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var command = _dataSource.CreateCommand($"SELECT {SelectColumns} FROM shared_jobs WHERE id = @id");
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<JobRecord>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        await using var command = _dataSource.CreateCommand($"SELECT {SelectColumns} FROM shared_jobs WHERE owner_id = @owner_id ORDER BY created_at");
        command.Parameters.AddWithValue("owner_id", ownerId);
        return await ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<JobRecord>> ClaimDueAsync(int max, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        // One atomic statement: the subquery locks the rows it selects with
        // SKIP LOCKED, so two workers — even in two processes — never claim
        // the same job.
        const string sql = """
            UPDATE shared_jobs AS j
            SET state = 'running', attempts = j.attempts + 1, updated_at = @now, next_attempt_at = NULL
            FROM (
                SELECT id FROM shared_jobs
                WHERE state = 'queued' AND (next_attempt_at IS NULL OR next_attempt_at <= @now)
                ORDER BY created_at
                LIMIT @max
                FOR UPDATE SKIP LOCKED
            ) AS due
            WHERE j.id = due.id
            RETURNING
                j.id, j.type, j.payload, j.owner_id, j.state, j.attempts, j.max_attempts, j.last_error, j.created_at, j.updated_at, j.next_attempt_at
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("now", now);
        command.Parameters.AddWithValue("max", max);
        return await ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(JobRecord job, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE shared_jobs
            SET state = @state, attempts = @attempts, last_error = @last_error, updated_at = @updated_at, next_attempt_at = @next_attempt_at
            WHERE id = @id
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", job.Id);
        command.Parameters.AddWithValue("state", job.State);
        command.Parameters.AddWithValue("attempts", job.Attempts);
        command.Parameters.Add(NullableText("last_error", job.LastError));
        command.Parameters.AddWithValue("updated_at", job.UpdatedAt);
        command.Parameters.Add(NullableTimestamp("next_attempt_at", job.NextAttemptAt));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddRecordParameters(NpgsqlCommand command, JobRecord job)
    {
        command.Parameters.AddWithValue("id", job.Id);
        command.Parameters.AddWithValue("type", job.Type);
        command.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = job.PayloadJson });
        command.Parameters.Add(NullableText("owner_id", job.OwnerId));
        command.Parameters.AddWithValue("state", job.State);
        command.Parameters.AddWithValue("attempts", job.Attempts);
        command.Parameters.AddWithValue("max_attempts", job.MaxAttempts);
        command.Parameters.Add(NullableText("last_error", job.LastError));
        command.Parameters.AddWithValue("created_at", job.CreatedAt);
        command.Parameters.AddWithValue("updated_at", job.UpdatedAt);
        command.Parameters.Add(NullableTimestamp("next_attempt_at", job.NextAttemptAt));
    }

    private static NpgsqlParameter NullableText(string name, string? value) =>
        new(name, NpgsqlDbType.Text) { Value = (object?)value ?? DBNull.Value };

    private static NpgsqlParameter NullableTimestamp(string name, DateTimeOffset? value) =>
        new(name, NpgsqlDbType.TimestampTz) { Value = (object?)value ?? DBNull.Value };

    private static async Task<IReadOnlyList<JobRecord>> ReadAllAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var results = new List<JobRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    private static JobRecord Map(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        Type = reader.GetString(1),
        PayloadJson = reader.GetString(2),
        OwnerId = reader.IsDBNull(3) ? null : reader.GetString(3),
        State = reader.GetString(4),
        Attempts = reader.GetInt32(5),
        MaxAttempts = reader.GetInt32(6),
        LastError = reader.IsDBNull(7) ? null : reader.GetString(7),
        CreatedAt = reader.GetFieldValue<DateTimeOffset>(8),
        UpdatedAt = reader.GetFieldValue<DateTimeOffset>(9),
        NextAttemptAt = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
    };
}
