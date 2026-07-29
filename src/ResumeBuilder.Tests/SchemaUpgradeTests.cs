using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Data;

namespace ResumeBuilder.Tests;

/// <summary>
/// The upgrade path for an install that predates a new table. This is the case with no CI
/// coverage by nature — it only happens on someone else's machine, with their data.
/// </summary>
public class SchemaUpgradeTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rb-upgrade-" + Guid.NewGuid().ToString("N"));

    public SchemaUpgradeTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private ResumeDbContext Open() =>
        new(new DbContextOptionsBuilder<ResumeDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_dir, "resumes.db")}").Options);

    [Fact]
    public void AnOlderDatabaseGainsTheTableAndKeepsItsResumes()
    {
        // Stand up a database as it existed before JobApplications, with a résumé in it.
        using (var old = Open())
        {
            old.Database.ExecuteSqlRaw("""
                CREATE TABLE "Resumes" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Resumes" PRIMARY KEY AUTOINCREMENT,
                    "Name" TEXT NOT NULL
                );
                """);
            old.Database.ExecuteSqlRaw("INSERT INTO \"Resumes\" (\"Name\") VALUES ('Do not lose me')");
        }

        using (var upgraded = Open())
        {
            DatabaseInitializer.Initialize(upgraded);

            // The new table exists...
            upgraded.JobApplications.Add(new JobApplication { Company = "Stripe" });
            upgraded.SaveChanges();
            upgraded.JobApplications.Should().ContainSingle();

            // ...and the pre-existing data is untouched.
            upgraded.Database
                .SqlQueryRaw<string>("SELECT \"Name\" AS \"Value\" FROM \"Resumes\"")
                .ToList()
                .Should().ContainSingle().Which.Should().Be("Do not lose me");
        }
    }

    [Fact]
    public void TheUpgradeTakesABackupFirst()
    {
        using (var old = Open())
        {
            old.Database.ExecuteSqlRaw("""
                CREATE TABLE "Resumes" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Resumes" PRIMARY KEY AUTOINCREMENT,
                    "Name" TEXT NOT NULL
                );
                """);
            old.Database.ExecuteSqlRaw("INSERT INTO \"Resumes\" (\"Name\") VALUES ('Irreplaceable')");
        }

        using (var upgraded = Open())
        {
            DatabaseInitializer.Initialize(upgraded);
        }

        // A hand-written schema step ran against real data; there must be a way back.
        var backups = DatabaseBackup.List(_dir);
        backups.Should().NotBeEmpty("a schema change must be recoverable");

        using var restored = new ResumeDbContext(new DbContextOptionsBuilder<ResumeDbContext>()
            .UseSqlite($"Data Source={backups[0]}").Options);

        restored.Database
            .SqlQueryRaw<string>("SELECT \"Name\" AS \"Value\" FROM \"Resumes\"")
            .ToList()
            .Should().ContainSingle().Which.Should().Be("Irreplaceable");
    }
}
