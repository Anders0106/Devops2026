using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Chirp.Razor;

/// <summary>
/// Migrates data from an existing SQLite database to PostgreSQL on first startup.
/// Runs only when the SQLite file exists and PostgreSQL has no data.
/// </summary>
public static class SqliteToPostgresMigrator
{
	private static readonly string[] TablesInOrder =
	[
		"AspNetRoles",
		"AspNetUsers",
		"AspNetRoleClaims",
		"AspNetUserClaims",
		"AspNetUserLogins",
		"AspNetUserRoles",
		"AspNetUserTokens",
		"Cheeps",
		"Follows",
		"Comments",
		"__EFMigrationsHistory"
	];

	public static async Task MigrateIfNeededAsync(
		Chirp.Repositories.Repositories.ChirpDBContext pgContext,
		string sqlitePath,
		ILogger? logger = null,
		CancellationToken cancellationToken = default)
	{
		var resolvedPath = ResolveSqlitePath(sqlitePath);
		if (resolvedPath == null)
		{
			logger?.LogDebug("SQLite migration skipped: no chirp.db found at {Path} (tried BaseDirectory, CurrentDirectory, /app)", sqlitePath);
			return;
		}

		logger?.LogInformation("Found SQLite database at {Path}, checking if migration needed", resolvedPath);

		var pgConn = (NpgsqlConnection)pgContext.Database.GetDbConnection();
		if (pgConn.State != System.Data.ConnectionState.Open)
			await pgConn.OpenAsync(cancellationToken);

		// Check if PostgreSQL already has data
		await using (var checkCmd = pgConn.CreateCommand())
		{
			checkCmd.CommandText = "SELECT COUNT(*) FROM \"AspNetUsers\"";
			var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken));
			if (count > 0)
			{
				logger?.LogDebug("SQLite migration skipped: PostgreSQL already has {Count} users", count);
				return;
			}
		}

		logger?.LogInformation("Migrating data from SQLite to PostgreSQL");

		// Ensure schema exists (Migrate should have run before this)
		var sqliteConnStr = $"Data Source={resolvedPath}";
		await using var sqliteConn = new SqliteConnection(sqliteConnStr);
		await sqliteConn.OpenAsync(cancellationToken);

		foreach (var tableName in TablesInOrder)
		{
			if (!await TableExistsAsync(sqliteConn, tableName, cancellationToken))
				continue;

			var columns = await GetColumnNamesAsync(sqliteConn, tableName, cancellationToken);
			if (columns.Count == 0)
				continue;

			await CopyTableAsync(sqliteConn, pgConn, tableName, columns, cancellationToken);
		}

		await UpdatePostgresSequencesAsync(pgConn, cancellationToken);
		logger?.LogInformation("SQLite to PostgreSQL migration completed successfully");
	}

	private static string? ResolveSqlitePath(string sqlitePath)
	{
		var pathsToTry = new List<string>();
		if (Path.IsPathRooted(sqlitePath))
			pathsToTry.Add(sqlitePath);
		else
		{
			pathsToTry.Add(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, sqlitePath)));
			pathsToTry.Add(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), sqlitePath)));
			pathsToTry.Add(Path.Combine("/app", sqlitePath));
		}

		foreach (var p in pathsToTry)
		{
			if (File.Exists(p))
				return p;
		}
		return null;
	}

	private static async Task<bool> TableExistsAsync(SqliteConnection conn, string tableName, CancellationToken ct)
	{
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
		cmd.Parameters.AddWithValue("@name", tableName);
		return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;
	}

	private static async Task<List<string>> GetColumnNamesAsync(SqliteConnection conn, string tableName, CancellationToken ct)
	{
		var columns = new List<string>();
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = $"PRAGMA table_info(\"{tableName}\")";
		await using var reader = await cmd.ExecuteReaderAsync(ct);
		while (await reader.ReadAsync(ct))
			columns.Add(reader.GetString(1));
		return columns;
	}

	private static async Task CopyTableAsync(
		SqliteConnection sqliteConn,
		NpgsqlConnection pgConn,
		string tableName,
		List<string> columns,
		CancellationToken ct)
	{
		var colList = string.Join(", ", columns.Select(c => $"\"{c}\""));
		var paramPlaceholders = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

		await using var selectCmd = sqliteConn.CreateCommand();
		selectCmd.CommandText = $"SELECT {colList} FROM \"{tableName}\"";

		await using var reader = await selectCmd.ExecuteReaderAsync(ct);

		await using var insertCmd = pgConn.CreateCommand();
		insertCmd.CommandText = $"INSERT INTO \"{tableName}\" ({colList}) VALUES ({paramPlaceholders})";

		for (var i = 0; i < columns.Count; i++)
			insertCmd.Parameters.Add(new NpgsqlParameter($"@p{i}", (object?)DBNull.Value));

		var batch = new List<object?[]>();
		const int batchSize = 100;

		while (await reader.ReadAsync(ct))
		{
			var values = new object?[columns.Count];
			for (var i = 0; i < columns.Count; i++)
				values[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);

			batch.Add(values);
			if (batch.Count >= batchSize)
				await ExecuteBatchAsync(insertCmd, batch, columns.Count, ct);
		}

		if (batch.Count > 0)
			await ExecuteBatchAsync(insertCmd, batch, columns.Count, ct);
	}

	private static async Task ExecuteBatchAsync(
		NpgsqlCommand insertCmd,
		List<object?[]> batch,
		int columnCount,
		CancellationToken ct)
	{
		await using var tx = await insertCmd.Connection!.BeginTransactionAsync(ct);
		insertCmd.Transaction = tx;

		foreach (var values in batch)
		{
			for (var i = 0; i < columnCount; i++)
				insertCmd.Parameters[i].Value = values[i];
			await insertCmd.ExecuteNonQueryAsync(ct);
		}

		await tx.CommitAsync(ct);
		batch.Clear();
	}

	private static async Task UpdatePostgresSequencesAsync(NpgsqlConnection pgConn, CancellationToken ct)
	{
		async Task SetSequenceAsync(string tableName, string columnName)
		{
			await using var maxCmd = pgConn.CreateCommand();
			maxCmd.CommandText = $"SELECT COALESCE(MAX(\"{columnName}\"), 0) FROM \"{tableName}\"";
			var max = Convert.ToInt32(await maxCmd.ExecuteScalarAsync(ct));
			if (max == 0) return;

			await using var seqCmd = pgConn.CreateCommand();
			seqCmd.CommandText = "SELECT setval(pg_get_serial_sequence(@table, @column)::regclass, @max)";
			seqCmd.Parameters.AddWithValue("@table", $"\"{tableName}\"");
			seqCmd.Parameters.AddWithValue("@column", columnName);
			seqCmd.Parameters.AddWithValue("@max", max);
			try
			{
				await seqCmd.ExecuteNonQueryAsync(ct);
			}
			catch (PostgresException)
			{
				// Sequence may not exist for this column
			}
		}

		await SetSequenceAsync("Cheeps", "CheepId");
		await SetSequenceAsync("Comments", "CommentId");
		await SetSequenceAsync("AspNetRoleClaims", "Id");
		await SetSequenceAsync("AspNetUserClaims", "Id");
	}
}
