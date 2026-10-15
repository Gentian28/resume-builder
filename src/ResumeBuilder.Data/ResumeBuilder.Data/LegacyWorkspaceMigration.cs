using System.Xml.Linq;
using Microsoft.Data.Sqlite;

namespace ResumeBuilder.Data;

/// <summary>Copies legacy user data out of the Windows install tree before it can be uninstalled.</summary>
public static class LegacyWorkspaceMigration
{
    /// <summary>
    /// Setup renames an existing install before invoking the new app's install hook,
    /// then deletes that rollback directory. Move the whole legacy tree outside
    /// its cleanup path before doing any slower database work on ordinary launch.
    /// </summary>
    public static void PreserveInstallerRollback(string installDirectory, string recoveryDirectory)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(installDirectory))!;
        var prefix = Path.GetFileName(installDirectory) + ".";
        foreach (var candidate in Directory.EnumerateDirectories(parent, prefix + "*"))
        {
            var name = Path.GetFileName(candidate);
            // Windows wildcard matching can return the extensionless install itself.
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var suffix = name[prefix.Length..];
            if (suffix.Length != 16 || !suffix.All(char.IsAsciiLetterOrDigit)
                || !File.Exists(Path.Combine(candidate, "resumes.db"))) continue;
            var manifest = Path.Combine(candidate, "current", "sq.version");
            if (!File.Exists(manifest)) continue;
            try
            {
                if (!XDocument.Load(manifest).Descendants().Any(e => e.Name.LocalName == "id" && e.Value == "ResumeBuilder")) continue;
            }
            catch (System.Xml.XmlException) { continue; }

            Directory.CreateDirectory(recoveryDirectory);
            Directory.Move(candidate, Path.Combine(recoveryDirectory, Guid.NewGuid().ToString("N")));
        }
    }

    /// <summary>Find the still-live legacy profile, or the newest preserved installer rollback.</summary>
    public static string FindSourceDirectory(string legacyDirectory, string recoveryDirectory)
    {
        if (File.Exists(Path.Combine(legacyDirectory, "resumes.db")) || !Directory.Exists(recoveryDirectory)) return legacyDirectory;
        return Directory.EnumerateDirectories(recoveryDirectory)
            .Where(p => File.Exists(Path.Combine(p, "resumes.db")))
            .OrderByDescending(p => File.GetLastWriteTimeUtc(Path.Combine(p, "resumes.db")))
            .FirstOrDefault() ?? legacyDirectory;
    }

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
                DataSource = sourcePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString()))
            using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = stagingPath,
                Pooling = false
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
