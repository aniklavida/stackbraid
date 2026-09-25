using System.Data;
using Microsoft.Data.SqlClient;
using StackBraid.Shared.Jobs;

namespace StackBraid.Database.SqlServer;

/// <summary>
/// The persisted <see cref="IJobStore"/>: a queued job is a row in
/// <c>shared_jobs</c>, so it is still there when the API process restarts.
/// This is the only place the job store knows it is SQL Server — the port it
/// implements lives in <c>Shared</c> and names no provider.
/// </summary>
public sealed class SqlServerJobStore : IJobStore
{
    private const string SelectColumns =
        "id, type, payload, owner_id, state, attempts, max_attempts, last_error, created_at, updated_at, next_attempt_at";

    private readonly string _connectionString;

    public SqlServerJobStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Creates the job table and its indexes if they do not exist — idempotent, safe to call on every startup.</summary>
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF OBJECT_ID(N'shared_jobs', N'U') IS NULL
            BEGIN
                CREATE TABLE shared_jobs (
                    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    type NVARCHAR(200) NOT NULL,
                    payload NVARCHAR(MAX) NOT NULL CONSTRAINT DF_shared_jobs_payload DEFAULT N'{}',
                    owner_id NVARCHAR(200) NULL,
                    state NVARCHAR(20) NOT NULL,
                    attempts INT NOT NULL CONSTRAINT DF_shared_jobs_attempts DEFAULT 0,
                    max_attempts INT NOT NULL CONSTRAINT DF_shared_jobs_max_attempts DEFAULT 3,
                    last_error NVARCHAR(MAX) NULL,
                    created_at DATETIMEOFFSET NOT NULL,
                    updated_at DATETIMEOFFSET NOT NULL,
                    next_attempt_at DATETIMEOFFSET NULL
                );
                CREATE INDEX ix_shared_jobs_state_next_attempt_at ON shared_jobs (state, next_attempt_at);
                CREATE INDEX ix_shared_jobs_owner_id ON shared_jobs (owner_id);
            END
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
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

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        AddRecordParameters(command, job);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<JobRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand($"SELECT {SelectColumns} FROM shared_jobs WHERE id = @id", connection);
        command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = id;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<JobRecord>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand($"SELECT {SelectColumns} FROM shared_jobs WHERE owner_id = @owner_id ORDER BY created_at", connection);
        command.Parameters.Add("@owner_id", SqlDbType.NVarChar, 200).Value = ownerId;
        return await ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<JobRecord>> ClaimDueAsync(int max, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        // One atomic statement: the READPAST/ROWLOCK hint on the subquery lets
        // two workers — even in two processes — skip a row another already
        // holds, so they never claim the same job.
        const string sql = """
            UPDATE shared_jobs
            SET state = N'running', attempts = attempts + 1, updated_at = @now, next_attempt_at = NULL
            OUTPUT INSERTED.id, INSERTED.type, INSERTED.payload, INSERTED.owner_id, INSERTED.state,
                   INSERTED.attempts, INSERTED.max_attempts, INSERTED.last_error, INSERTED.created_at,
                   INSERTED.updated_at, INSERTED.next_attempt_at
            WHERE id IN (
                SELECT TOP (@max) id
                FROM shared_jobs WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE state = N'queued' AND (next_attempt_at IS NULL OR next_attempt_at <= @now)
                ORDER BY created_at
            )
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@now", SqlDbType.DateTimeOffset).Value = now;
        command.Parameters.Add("@max", SqlDbType.Int).Value = max;
        return await ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(JobRecord job, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE shared_jobs
            SET state = @state, attempts = @attempts, last_error = @last_error, updated_at = @updated_at, next_attempt_at = @next_attempt_at
            WHERE id = @id
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = job.Id;
        command.Parameters.Add("@state", SqlDbType.NVarChar, 20).Value = job.State;
        command.Parameters.Add("@attempts", SqlDbType.Int).Value = job.Attempts;
        command.Parameters.Add(NullableText("@last_error", job.LastError));
        command.Parameters.Add("@updated_at", SqlDbType.DateTimeOffset).Value = job.UpdatedAt;
        command.Parameters.Add(NullableTimestamp("@next_attempt_at", job.NextAttemptAt));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddRecordParameters(SqlCommand command, JobRecord job)
    {
        command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = job.Id;
        command.Parameters.Add("@type", SqlDbType.NVarChar, 200).Value = job.Type;
        command.Parameters.Add("@payload", SqlDbType.NVarChar, -1).Value = job.PayloadJson;
        command.Parameters.Add(NullableText("@owner_id", job.OwnerId));
        command.Parameters.Add("@state", SqlDbType.NVarChar, 20).Value = job.State;
        command.Parameters.Add("@attempts", SqlDbType.Int).Value = job.Attempts;
        command.Parameters.Add("@max_attempts", SqlDbType.Int).Value = job.MaxAttempts;
        command.Parameters.Add(NullableText("@last_error", job.LastError));
        command.Parameters.Add("@created_at", SqlDbType.DateTimeOffset).Value = job.CreatedAt;
        command.Parameters.Add("@updated_at", SqlDbType.DateTimeOffset).Value = job.UpdatedAt;
        command.Parameters.Add(NullableTimestamp("@next_attempt_at", job.NextAttemptAt));
    }

    private static SqlParameter NullableText(string name, string? value) =>
        new(name, SqlDbType.NVarChar, -1) { Value = (object?)value ?? DBNull.Value };

    private static SqlParameter NullableTimestamp(string name, DateTimeOffset? value) =>
        new(name, SqlDbType.DateTimeOffset) { Value = (object?)value ?? DBNull.Value };

    private static async Task<IReadOnlyList<JobRecord>> ReadAllAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        var results = new List<JobRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    private static JobRecord Map(SqlDataReader reader) => new()
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
