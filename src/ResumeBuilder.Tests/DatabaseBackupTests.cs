using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Data;

namespace ResumeBuilder.Tests;

/// <summary>
/// A schema upgrade is the one moment this app rewrites a file holding work nobody can retype from
/// memory. These pin that a backup is taken then, that it actually contains the data, and that
/// backups do not accumulate without limit.
/// </summary>
public class DatabaseBackupTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rb-backup-" + Guid.NewGuid().ToString("N"));

    public DatabaseBackupTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private ResumeDbContext Open(string file) =>
        new(new DbContextOptionsBuilder<ResumeDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_dir, file)}")
            .Options);

    [Fact]
    public void Create_CopiesDataThatIsStillInTheWriteAheadLog()
    {
        // The failure this class exists to prevent: SQLite in WAL mode keeps recent writes in a
        // side file, so a naive copy of the .db can capture a database missing everything since
        // the last checkpoint. Without the checkpoint in Create(), this test fails - the backup
        // exists but the résumé is not in it.
        using (var db = Open("resumes.db"))
        {
            db.Database.EnsureCreated();
            db.Resumes.Add(new Resume { Name = "Irreplaceable", TargetRole = "Senior Engineer" });
            db.SaveChanges();

            DatabaseBackup.Create(db).Should().NotBeNull();
        }

        var backup = DatabaseBackup.List(_dir).Single();

        using var restored = new ResumeDbContext(new DbContextOptionsBuilder<ResumeDbContext>()
            .UseSqlite($"Data Source={backup}").Options);

        restored.Resumes.Select(r => r.Name).Should().ContainSingle().Which.Should().Be("Irreplaceable");
    }

    [Fact]
    public void Create_KeepsOnlyTheMostRecentFew()
    {
        using var db = Open("resumes.db");
        db.Database.EnsureCreated();

        for (var i = 0; i < DatabaseBackup.KeepCount + 3; i++)
        {
            // Timestamps have one-second resolution, so distinct names are forced by hand rather
            // than by sleeping through the test.
            var made = DatabaseBackup.Create(db);
            if (made != null && File.Exists(made))
            {
                File.Move(made, Path.Combine(_dir, $"resumes.backup-2020010{i}-000000.db"), overwrite: true);
            }
        }

        DatabaseBackup.Create(db);

        DatabaseBackup.List(_dir).Count.Should().BeLessThanOrEqualTo(DatabaseBackup.KeepCount);
    }

    [Fact]
    public void Create_OnAnInMemoryDatabase_DoesNothingRatherThanThrowing()
    {
        using var db = new ResumeDbContext(new DbContextOptionsBuilder<ResumeDbContext>()
            .UseSqlite("Data Source=:memory:").Options);

        DatabaseBackup.Create(db).Should().BeNull();
    }

    [Fact]
    public void Initialize_OnAnUpToDateDatabase_TakesNoBackup()
    {
        // Backing up on every launch would copy the database forever for no benefit. Only a
        // pending schema change is worth a copy.
        using (var first = Open("resumes.db"))
        {
            DatabaseInitializer.Initialize(first);
        }

        var afterFirst = DatabaseBackup.List(_dir).Count;

        using (var second = Open("resumes.db"))
        {
            DatabaseInitializer.Initialize(second);
        }

        DatabaseBackup.List(_dir).Count.Should().Be(afterFirst,
            "a second start with no schema change should not produce another backup");
    }
}
