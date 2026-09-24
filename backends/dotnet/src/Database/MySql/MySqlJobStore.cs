using System.Data;
using MySqlConnector;
using StackBraid.Shared.Jobs;

namespace StackBraid.Database.MySql;

/// <summary>
/// The persisted <see cref="IJobStore"/>: a queued job is a row in
/// <c>shared_jobs</c>, so it is still there when the API process restarts.
/// This is the only place the job store knows it is MySQL — the port it
/// implements lives in <c>Shared</c> and names no provider.
/// </summary>
public sealed class MySqlJobStore : IJobStore
{
    private const string SelectColumns =
        "id, type, payload, owner_id, state, attempts, max_attempts, last_error, created_at, updated_at, next_attempt_at";

    private readonly string _connectionString;

    public MySqlJobStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Creates the job table and its indexes if they do not exist — idempotent, safe to call on every startup.</summary>
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS shared_jobs (
                id CHAR(36) NOT NULL PRIMARY KEY,
                type VARCHAR(200) NOT NULL,
                payload JSON NOT NULL,
                owner_id VARCHAR(200) NULL,
                state VARCHAR(20) NOT NULL,
                attempts INT NOT NULL DEFAULT 0,
                max_attempts INT NOT NULL DEFAULT 3,
                last_error LONGTEXT NULL,
                created_at DATETIME(6) NOT NULL,
                updated_at DATETIME(6) NOT NULL,
                next_attempt_at DATETIME(6) NULL,
                INDEX ix_shared_jobs_state_next_attempt_at (state, next_attempt_at),
                INDEX ix_shared_jobs_owner_id (owner_id)
            )
            """;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new MySqlCommand(sql, connection);
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

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new MySqlCommand(sql, connection);
        AddRecordParameters(command, job);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<JobRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new MySqlCommand($"SELECT {SelectColumns} FROM shared_jobs WHERE id = @id", connection);
        command.Parameters.Add("@id", MySqlDbType.VarChar, 36).Value = id.ToString();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<JobRecord>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new MySqlCommand($"SELECT {SelectColumns} FROM shared_jobs WHERE owner_id = @owner_id ORDER BY created_at", connection);
        command.Parameters.Add("@owner_id", MySqlDbType.VarChar, 200).Value = ownerId;
        return await ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<JobRecord>> ClaimDueAsync(int max, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        // MySQL's optimizer rejects a subquery that selects from the table an
        // UPDATE writes to unless it is wrapped in a derived table; the
        // FOR UPDATE SKIP LOCKED inside that derived table is what stops two
        // workers — even in two processes — claiming the same job.
        const string sql = """
            UPDATE shared_jobs
            SET state = 'running', attempts = attempts + 1, updated_at = @now, next_attempt_at = NULL
            WHERE id IN (
                SELECT id FROM (
                    SELECT id FROM shared_jobs
                    WHERE state = 'queued' AND (next_attempt_at IS NULL OR next_attempt_at <= @now)
                    ORDER BY created_at
                    LIMIT @max
                    FOR UPDATE SKIP LOCKED
                ) AS due
            )
            """;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.Add("@now", MySqlDbType.DateTime).Value = now.UtcDateTime;
        command.Parameters.Add("@max", MySqlDbType.Int32).Value = max;

        var results = new List<JobRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public async Task UpdateAsync(JobRecord job, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE shared_jobs
            SET state = @state, attempts = @attempts, last_error = @last_error, updated_at = @updated_at, next_attempt_at = @next_attempt_at
            WHERE id = @id
            """;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.Add("@id", MySqlDbType.VarChar, 36).Value = job.Id.ToString();
        command.Parameters.Add("@state", MySqlDbType.VarChar, 20).Value = job.State;
        command.Parameters.Add("@attempts", MySqlDbType.Int32).Value = job.Attempts;
        command.Parameters.Add(NullableText("@last_error", job.LastError));
        command.Parameters.Add("@updated_at", MySqlDbType.DateTime).Value = job.UpdatedAt.UtcDateTime;
        command.Parameters.Add(NullableTimestamp("@next_attempt_at", job.NextAttemptAt));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddRecordParameters(MySqlCommand command, JobRecord job)
    {
        command.Parameters.Add("@id", MySqlDbType.VarChar, 36).Value = job.Id.ToString();
        command.Parameters.Add("@type", MySqlDbType.VarChar, 200).Value = job.Type;
        command.Parameters.Add("@payload", MySqlDbType.JSON).Value = job.PayloadJson;
        command.Parameters.Add(NullableText("@owner_id", job.OwnerId));
        command.Parameters.Add("@state", MySqlDbType.VarChar, 20).Value = job.State;
        command.Parameters.Add("@attempts", MySqlDbType.Int32).Value = job.Attempts;
        command.Parameters.Add("@max_attempts", MySqlDbType.Int32).Value = job.MaxAttempts;
        command.Parameters.Add(NullableText("@last_error", job.LastError));
        command.Parameters.Add("@created_at", MySqlDbType.DateTime).Value = job.CreatedAt.UtcDateTime;
        command.Parameters.Add("@updated_at", MySqlDbType.DateTime).Value = job.UpdatedAt.UtcDateTime;
        command.Parameters.Add(NullableTimestamp("@next_attempt_at", job.NextAttemptAt));
    }

    private static MySqlParameter NullableText(string name, string? value) =>
        new(name, MySqlDbType.LongText) { Value = (object?)value ?? DBNull.Value };

    private static MySqlParameter NullableTimestamp(string name, DateTimeOffset? value) =>
        new(name, MySqlDbType.DateTime) { Value = value is null ? DBNull.Value : value.Value.UtcDateTime };

    private static async Task<IReadOnlyList<JobRecord>> ReadAllAsync(MySqlCommand command, CancellationToken cancellationToken)
    {
        var results = new List<JobRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    private static JobRecord Map(MySqlDataReader reader) => new()
    {
        Id = Guid.Parse(reader.GetString(0)),
        Type = reader.GetString(1),
        PayloadJson = reader.GetString(2),
        OwnerId = reader.IsDBNull(3) ? null : reader.GetString(3),
        State = reader.GetString(4),
        Attempts = reader.GetInt32(5),
        MaxAttempts = reader.GetInt32(6),
        LastError = reader.IsDBNull(7) ? null : reader.GetString(7),
        CreatedAt = AsUtc(reader.GetDateTime(8)),
        UpdatedAt = AsUtc(reader.GetDateTime(9)),
        NextAttemptAt = reader.IsDBNull(10) ? null : AsUtc(reader.GetDateTime(10)),
    };

    private static DateTimeOffset AsUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
