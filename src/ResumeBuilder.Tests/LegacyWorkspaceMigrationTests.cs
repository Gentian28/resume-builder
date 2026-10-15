using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using ResumeBuilder.Data;

namespace ResumeBuilder.Tests;

public sealed class LegacyWorkspaceMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "rb-relocate-" + Guid.NewGuid().ToString("N"));
    private string Source => Path.Combine(_root, "installed");
    private string Destination => Path.Combine(_root, "data");

    public LegacyWorkspaceMigrationTests() => Directory.CreateDirectory(Source);

    private static SqliteConnection Open(string directory)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(directory, "resumes.db"),
            Pooling = false
        }.ToString());
        connection.Open();
        return connection;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [Fact]
    public void Copy_IncludesCommittedWalAndKnownUserFilesButLeavesInstallerAndSourceAlone()
    {
        using var source = Open(Source);
        Execute(source, "PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; CREATE TABLE Payload(Value TEXT); INSERT INTO Payload VALUES('Synthetic saved work');");
        File.Exists(Path.Combine(Source, "resumes.db-wal")).Should().BeTrue();
        File.WriteAllText(Path.Combine(Source, "settings.json"), "{\"theme\":\"dark\"}");
        File.WriteAllText(Path.Combine(Source, "sync-state.json.destination.state"), "{}");
        File.WriteAllText(Path.Combine(Source, "Update.exe"), "installer fixture");

        LegacyWorkspaceMigration.CopyIfNeeded(Source, Destination).Should().BeTrue();

        using var copy = Open(Destination);
        using var query = copy.CreateCommand();
        query.CommandText = "SELECT Value FROM Payload";
        query.ExecuteScalar().Should().Be("Synthetic saved work");
        File.ReadAllText(Path.Combine(Destination, "settings.json")).Should().Contain("dark");
        File.Exists(Path.Combine(Destination, "sync-state.json.destination.state")).Should().BeTrue();
        File.Exists(Path.Combine(Destination, "Update.exe")).Should().BeFalse();
        File.Exists(Path.Combine(Source, "resumes.db")).Should().BeTrue();
    }

    [Fact]
    public void ExistingDestination_IsNeverReplaced()
    {
        File.WriteAllText(Path.Combine(Source, "resumes.db"), "legacy fixture");
        Directory.CreateDirectory(Destination);
        var target = Path.Combine(Destination, "resumes.db");
        File.WriteAllText(target, "newer workspace fixture");

        LegacyWorkspaceMigration.CopyIfNeeded(Source, Destination).Should().BeFalse();
        File.ReadAllText(target).Should().Be("newer workspace fixture");
    }

    [Fact]
    public void CorruptSource_DoesNotPublishAnEmptyWorkspace()
    {
        File.WriteAllText(Path.Combine(Source, "resumes.db"), "not a database");
        var migrate = () => LegacyWorkspaceMigration.CopyIfNeeded(Source, Destination);
        migrate.Should().Throw<SqliteException>();
        File.Exists(Path.Combine(Destination, "resumes.db")).Should().BeFalse();
        Directory.EnumerateFiles(Destination).Should().BeEmpty();
    }

    [Fact]
    public void ExistingSettings_ArePreservedWhenTheDatabaseHasNotYetBeenCopied()
    {
        using var source = Open(Source);
        Execute(source, "CREATE TABLE Payload(Value TEXT);");
        File.WriteAllText(Path.Combine(Source, "settings.json"), "old");
        Directory.CreateDirectory(Destination);
        File.WriteAllText(Path.Combine(Destination, "settings.json"), "new");

        LegacyWorkspaceMigration.CopyIfNeeded(Source, Destination).Should().BeTrue();
        LegacyWorkspaceMigration.CopyIfNeeded(Source, Destination).Should().BeFalse();
        File.ReadAllText(Path.Combine(Destination, "settings.json")).Should().Be("new");
    }

    [Fact]
    public void MissingSource_DoesNotCreateAnUnnecessaryDestination()
    {
        LegacyWorkspaceMigration.CopyIfNeeded(Source, Destination).Should().BeFalse();
        Directory.Exists(Destination).Should().BeFalse();
    }

    [Fact]
    public void InstallerRollback_IsMovedOutsideCleanupBeforeDatabaseMigration()
    {
        var rollback = Source + ".0123456789ABCDEF";
        Directory.CreateDirectory(Path.Combine(rollback, "current"));
        File.WriteAllText(Path.Combine(rollback, "current", "sq.version"), "<package><metadata><id>ResumeBuilder</id></metadata></package>");
        using (var db = Open(rollback)) Execute(db, "CREATE TABLE Payload(Value TEXT); INSERT INTO Payload VALUES('kept');");
        var recovery = Path.Combine(_root, "recovery");

        LegacyWorkspaceMigration.PreserveInstallerRollback(Source, recovery);

        Directory.Exists(rollback).Should().BeFalse();
        var preserved = Directory.EnumerateDirectories(recovery).Single();
        File.Exists(Path.Combine(preserved, "resumes.db")).Should().BeTrue();
        LegacyWorkspaceMigration.FindSourceDirectory(Source, recovery).Should().Be(preserved);
        LegacyWorkspaceMigration.CopyIfNeeded(preserved, Destination).Should().BeTrue();
        using var copy = Open(Destination);
        using var query = copy.CreateCommand();
        query.CommandText = "SELECT Value FROM Payload";
        query.ExecuteScalar().Should().Be("kept");
    }

    [Fact]
    public void InstallerRollback_IgnoresUnrelatedDirectoriesAndLiveProfileWins()
    {
        var unrelated = Source + ".0123456789ABCDEF";
        Directory.CreateDirectory(Path.Combine(unrelated, "current"));
        File.WriteAllText(Path.Combine(unrelated, "current", "sq.version"), "<package><metadata><id>AnotherApp</id></metadata></package>");
        File.WriteAllText(Path.Combine(unrelated, "resumes.db"), "unrelated fixture");
        var recovery = Path.Combine(_root, "recovery");

        LegacyWorkspaceMigration.PreserveInstallerRollback(Source, recovery);

        Directory.Exists(unrelated).Should().BeTrue();
        Directory.Exists(recovery).Should().BeFalse();
        using var live = Open(Source);
        Execute(live, "CREATE TABLE Payload(Value TEXT);");
        LegacyWorkspaceMigration.FindSourceDirectory(Source, recovery).Should().Be(Source);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
