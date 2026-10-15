using Microsoft.Data.Sqlite;

namespace ResumeBuilder.Data;

/// <summary>Copies legacy user data out of the Windows install tree before it can be uninstalled.</summary>
public static class LegacyWorkspaceMigration
{
    /// <summary>
    /// Publishes a consistent database snapshot last, leaving the source intact and never
    /// replacing a destination workspace. Only known user files cross, not installer content.
    /// </summary>
    public static bool CopyIfNeeded(string sourceDirectory, string destinationDirectory)
    {
        var sourcePath = Path.Combine(sourceDirectory, "resumes.db");
        var destinationPath = Path.Combine(destinationDirectory, "resumes.db");
        if (File.Exists(destinationPath) || !File.Exists(sourcePath)) return false;

        Directory.CreateDirectory(destinationDirectory);
        var stagingPath = Path.Combine(destinationDirectory, $".resumes-migration-{Guid.NewGuid():N}.db");
        try
        {
            using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = sourcePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false
            }.ToString()))
            using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = stagingPath, Pooling = false
            }.ToString()))
            {
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);
                using var check = destination.CreateCommand();
                check.CommandText = "PRAGMA quick_check";
                if (!Equals(check.ExecuteScalar(), "ok")) throw new IOException("The legacy workspace is not a valid database.");
            }

            var userFiles = new[] { "settings.json", "preferences.json" }
                .Select(name => Path.Combine(sourceDirectory, name))
                .Concat(Directory.EnumerateFiles(sourceDirectory, "sync-state.json*"))
                .Concat(Directory.EnumerateFiles(sourceDirectory, "resumes.backup-*.db"));
            foreach (var sourceFile in userFiles.Where(File.Exists))
            {
                var target = Path.Combine(destinationDirectory, Path.GetFileName(sourceFile));
                try { File.Copy(sourceFile, target, overwrite: false); }
                catch (IOException) when (File.Exists(target)) { /* Keep any newer destination settings. */ }
            }

            try { File.Move(stagingPath, destinationPath, overwrite: false); }
            catch (IOException) when (File.Exists(destinationPath)) { return false; }
            return true;
        }
        finally
        {
            if (File.Exists(stagingPath)) File.Delete(stagingPath);
        }
    }
}
