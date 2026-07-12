using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace ResumeBuilder.Data;

/// <summary>
/// Creates the schema and brings databases created by older versions up to date.
/// The project has no EF migrations history, so upgrades are applied as idempotent steps:
/// missing tables are created from the current model, and missing columns are added.
/// </summary>
public static class DatabaseInitializer
{
    private const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    /// <summary>SQLite has no uuid() function; this builds a v4 guid per row from randomblob().</summary>
    private const string RandomGuidSql =
        "lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)), 2) || " +
        "'-' || substr('89ab', abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)), 2) || '-' || hex(randomblob(6)))";

    /// <summary>Columns added after the first release, per table.</summary>
    private static readonly Dictionary<string, (string Column, string Ddl)[]> AddedColumns = new()
    {
        ["Resumes"] = new[]
        {
            ("RowVersion", $"TEXT NOT NULL DEFAULT '{EmptyGuid}'"),
            ("SyncId", $"TEXT NOT NULL DEFAULT '{EmptyGuid}'"),
            ("BaseResumeId", "INTEGER NULL"),
            ("TargetRole", "TEXT NOT NULL DEFAULT ''"),
            ("JobDescription", "TEXT NOT NULL DEFAULT ''")
        }
    };

    public static void Initialize(ResumeDbContext context)
    {
        context.Database.EnsureCreated();
        CreateMissingTables(context);
        AddMissingColumns(context);
        BackfillSyncIds(context);
    }

    /// <summary>
    /// EnsureCreated is a no-op once the file exists, so a table added in a later version would
    /// never appear on an existing install. Create those from the model's own DDL.
    /// </summary>
    private static void CreateMissingTables(ResumeDbContext context)
    {
        var existing = ExistingTables(context);
        var script = context.Database.GenerateCreateScript();

        foreach (var statement in SplitStatements(script))
        {
            var table = TableBeingCreated(statement);
            if (table == null || existing.Contains(table))
            {
                continue;
            }

            context.Database.ExecuteSqlRaw(statement);
            existing.Add(table);
        }

        // Indexes for the tables we just created come as separate statements.
        foreach (var statement in SplitStatements(script))
        {
            var indexed = TableBeingIndexed(statement);
            if (indexed == null || !existing.Contains(indexed))
            {
                continue;
            }

            try
            {
                context.Database.ExecuteSqlRaw(statement);
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                // Index already exists - nothing to do.
            }
        }
    }

    private static void AddMissingColumns(ResumeDbContext context)
    {
        foreach (var (table, columns) in AddedColumns)
        {
            var existing = ExistingColumns(context, table);
            if (existing.Count == 0)
            {
                continue;
            }

            foreach (var (column, ddl) in columns)
            {
                if (existing.Contains(column))
                {
                    continue;
                }

                // Table/column/type all come from the AddedColumns literal above, and SQL cannot
                // parameterize identifiers or DDL types anyway - there is no caller input here.
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                context.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {ddl}");
#pragma warning restore EF1002
            }
        }
    }

    /// <summary>Gives resumes saved before sync existed a stable identity, once.</summary>
    private static void BackfillSyncIds(ResumeDbContext context)
    {
        // Every fragment here is a compile-time constant - no caller input reaches this SQL.
        const string sql =
            "UPDATE \"Resumes\" SET \"SyncId\" = " + RandomGuidSql +
            " WHERE \"SyncId\" = '" + EmptyGuid + "' OR \"SyncId\" IS NULL";

        context.Database.ExecuteSqlRaw(sql);
    }

    private static IEnumerable<string> SplitStatements(string script) =>
        script.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0);

    private static string? TableBeingCreated(string statement)
    {
        var match = Regex.Match(statement, @"^CREATE\s+TABLE\s+""(?<name>[^""]+)""", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static string? TableBeingIndexed(string statement)
    {
        var match = Regex.Match(
            statement,
            @"^CREATE\s+(UNIQUE\s+)?INDEX\s+""[^""]+""\s+ON\s+""(?<table>[^""]+)""",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["table"].Value : null;
    }

    private static HashSet<string> ExistingTables(ResumeDbContext context)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Query(context, "SELECT name FROM sqlite_master WHERE type = 'table'", reader => tables.Add(reader.GetString(0)));
        return tables;
    }

    private static HashSet<string> ExistingColumns(ResumeDbContext context, string table)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Query(context, $"PRAGMA table_info(\"{table}\")", reader => columns.Add(reader.GetString(1)));
        return columns;
    }

    private static void Query(ResumeDbContext context, string sql, Action<System.Data.Common.DbDataReader> onRow)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                onRow(reader);
            }
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }
}
