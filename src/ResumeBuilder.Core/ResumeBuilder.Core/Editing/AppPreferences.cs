using System.Text.Json;
using ResumeBuilder.Core.SmartContent;
using ResumeBuilder.Core.Sync;

namespace ResumeBuilder.Core.Editing;

/// <summary>Only non-secret preferences are serializable here.</summary>
public sealed class AppPreferences
{
    public AiProvider AiProvider { get; set; }
    public string AiBaseUrl { get; set; } = LocalAiService.OpenAiBaseUrl;
    public string OpenAiModel { get; set; } = LocalAiService.DefaultModel;
    public string AnthropicModel { get; set; } = AnthropicAiService.DefaultModel;
    public string? SyncFolder { get; set; }
    public bool SyncEnabled { get; set; } = true;
    public ConflictResolution ConflictResolution { get; set; }
    public int? LastResumeId { get; set; }
}

public sealed class AppPreferencesStore(string path)
{
    public AppPreferences Load()
    {
        try
        {
            return JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(path)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new();
        }
    }

    public void Save(AppPreferences preferences)
    {
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        // A synchronous small write keeps preference changes ordered on the UI thread.
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(preferences));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
