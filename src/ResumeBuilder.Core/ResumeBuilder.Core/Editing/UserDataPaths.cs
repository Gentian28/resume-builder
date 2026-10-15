namespace ResumeBuilder.Core.Editing;

public static class UserDataPaths
{
    /// <summary>An explicit override allows clean-profile launch tests without touching personal work.</summary>
    public static string Directory => Environment.GetEnvironmentVariable("RESUMEBUILDER_DATA_DIRECTORY") is { Length: > 0 } path
        ? Path.GetFullPath(path)
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify), "ResumeBuilder");
}
