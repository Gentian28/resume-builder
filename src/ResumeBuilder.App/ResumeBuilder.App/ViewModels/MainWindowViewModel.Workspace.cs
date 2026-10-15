using System.Threading.Tasks;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Data;

namespace ResumeBuilder.App.ViewModels;

public partial class MainWindowViewModel
{
    private async Task<bool> SaveWorkspaceChangesAsync()
    {
        if (IsDirty && !await SaveAsync(isAutoSave: false)) return false;
        if (IsLetterDirty)
        {
            await SaveCoverLetterAsync();
            if (IsLetterDirty) return false;
        }
        return await FlushApplicationsAsync();
    }

    private async Task RestoreWorkspaceAsync(WorkspaceArchive archive)
    {
        if (_services.WorkspaceBackups is not { } backups) return;
        if (IsSyncing)
        {
            await Dialogs.ShowErrorAsync("Sync is still running", "Wait for folder sync to finish before restoring a workspace backup.");
            return;
        }
        if (!await ConfirmDiscardLetterChangesAsync() || !await FlushApplicationsAsync()) return;
        if (!await Dialogs.ConfirmAsync("Restore workspace backup",
            $"Replace this computer's workspace with {archive.Resumes.Count} resumes, {archive.CoverLetters.Count} cover letters and {archive.Applications.Count} applications? " +
            "A safety copy of the current database will be kept before replacement. Folder sync will be paused.", "Restore", "Cancel")) return;

        await _saveGate.WaitAsync();
        await _letterSaveGate.WaitAsync();
        try
        {
            foreach (var row in Applications) await row.StopSavingAsync();
            var safetyCopy = await System.Threading.Tasks.Task.Run(() => backups.RestoreAsync(archive));
            ResetToNewResume();
            LoadLetterIntoEditor(new CoverLetter());
            _letterEdits.Reset();
            IsLetterDirty = false;
            ShowCoverLetterEditor = false;
            ShowCoverLetterList = false;
            ShowApplications = false;
            IsSyncConfigured = false;
            PersistPreferences();
            SyncStatusText = "Paused after restore. Configure a sync folder when ready.";
            await LoadSavedResumesAsync();
            await LoadCoverLettersAsync();
            await LoadApplicationsAsync();
            StatusMessage = safetyCopy is null ? "Workspace restored" : $"Workspace restored. Previous database: {safetyCopy}";
        }
        catch
        {
            await LoadApplicationsAsync();
            throw;
        }
        finally
        {
            _letterSaveGate.Release();
            _saveGate.Release();
        }
    }
}
