using System;
using System.IO;
using System.Threading.Tasks;
using ResumeBuilder.Core.Editing;
using ResumeBuilder.Core.SmartContent;

namespace ResumeBuilder.App.ViewModels;

public partial class MainWindowViewModel
{
    private readonly AppPreferencesStore _preferencesStore = new(Path.Combine(UserDataPaths.Directory, "preferences.json"));
    private bool _persistPreferences;

    private async Task RestorePreferencesAsync()
    {
        var preferences = _preferencesStore.Load();
        if (_services.AiService is AiProviderRouter router)
        {
            try
            {
                router.OpenAiCompatible.ConfigureEndpoint(preferences.AiBaseUrl, preferences.OpenAiModel, null);
                router.Anthropic.Configure("", preferences.AnthropicModel);
                router.Active = preferences.AiProvider;
                IsAnthropicSelected = preferences.AiProvider == AiProvider.Anthropic;
                AiBaseUrl = preferences.AiBaseUrl;
                AiModel = IsAnthropicSelected ? preferences.AnthropicModel : preferences.OpenAiModel;
            }
            catch (ArgumentException) { }
            UpdateAiPrivacyNotice();
        }

        SelectedConflictResolution = Enum.IsDefined(preferences.ConflictResolution) ? preferences.ConflictResolution : default;
        if (!string.IsNullOrWhiteSpace(preferences.SyncFolder))
        {
            SyncFolderPath = preferences.SyncFolder;
            if (preferences.SyncEnabled && Directory.Exists(preferences.SyncFolder)) await ConfigureSyncAsync();
        }
        if (preferences.LastResumeId is { } id && !IsDirty)
        {
            var resume = await _services.Repository.GetByIdAsync(id);
            if (resume != null && !IsDirty) await LoadResumeAsync(resume);
        }
        _persistPreferences = true;
    }

    private void PersistPreferences()
    {
        if (!_persistPreferences || _services.AiService is not AiProviderRouter router) return;
        try
        {
            _preferencesStore.Save(new AppPreferences
            {
                AiProvider = router.Active,
                AiBaseUrl = router.OpenAiCompatible.BaseUrl,
                OpenAiModel = router.OpenAiCompatible.Model,
                AnthropicModel = router.Anthropic.Model,
                SyncFolder = SyncFolderPath,
                SyncEnabled = IsSyncConfigured,
                ConflictResolution = SelectedConflictResolution,
                LastResumeId = CurrentResume.Id == 0 ? null : CurrentResume.Id
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusMessage = "Preferences could not be saved. Your documents are unaffected.";
        }
    }
}
