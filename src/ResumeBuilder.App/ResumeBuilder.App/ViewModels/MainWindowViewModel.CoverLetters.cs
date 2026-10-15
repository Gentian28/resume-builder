using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ResumeBuilder.App.Services;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.UndoRedo;

namespace ResumeBuilder.App.ViewModels;

public partial class MainWindowViewModel
{
    private const string DefaultLetterName = "Untitled Cover Letter";

    private DispatcherTimer? _letterPreviewTimer;
    private CancellationTokenSource? _letterPreviewCts;
    private string? _letterRenderedSignature;
    private bool _isLoadingLetter;
    private readonly SemaphoreSlim _letterSaveGate = new(1, 1);
    private CancellationTokenSource? _letterDraftCts;
    private readonly UndoRedoManager _letterUndoManager = new();

    partial void OnShowCoverLetterEditorChanged(bool value) => RefreshActiveUndoState();

    private void RecordLetterEdit(string key, string oldValue, string newValue, Action<string> setter)
    {
        if (_isLoadingLetter || _letterUndoManager.IsExecutingAction || oldValue == newValue) return;
        _letterUndoManager.RecordAction(new TextEditAction(key, oldValue, newValue, setter, "Edit letter"));
    }

    private sealed class LetterEditRecorder(MainWindowViewModel owner) : ITextEditRecorder
    {
        public void RecordTextEdit(string fieldKey, string oldValue, string newValue, Action<string> setter, string description) =>
            owner.RecordLetterEdit(fieldKey, oldValue, newValue, setter);
    }

    private LetterParagraphViewModel CreateLetterParagraph(string text) => new(text, OnLetterEditorChanged, new LetterEditRecorder(this));

    [ObservableProperty]
    private bool _showCoverLetterEditor;

    [ObservableProperty]
    private bool _showCoverLetterList;

    [ObservableProperty]
    private ObservableCollection<CoverLetter> _coverLetters = new();

    [ObservableProperty]
    private CoverLetter _currentLetter = new();

    [ObservableProperty]
    private bool _isLetterDirty;

    private readonly ResumeBuilder.Core.Editing.DirtyTracker _letterEdits = new();

    [ObservableProperty]
    private bool _isDraftingLetter;

    [ObservableProperty]
    private string _letterSaveStateText = "No changes";

    [ObservableProperty]
    private ObservableCollection<Bitmap> _letterPreviewPages = new();

    [ObservableProperty]
    private string _letterName = DefaultLetterName;

    [ObservableProperty]
    private string _letterRecipientName = "";

    [ObservableProperty]
    private string _letterRecipientTitle = "";

    [ObservableProperty]
    private string _letterCompanyName = "";

    [ObservableProperty]
    private string _letterCompanyAddress = "";

    [ObservableProperty]
    private DateTimeOffset _letterDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string _letterSubject = "";

    [ObservableProperty]
    private string _letterSalutation = "";

    [ObservableProperty]
    private string _letterSalutationPlaceholder = "Dear Hiring Manager,";

    [ObservableProperty]
    private string _letterClosing = "Sincerely,";

    [ObservableProperty]
    private ObservableCollection<LetterParagraphViewModel> _letterParagraphs = new();

    // ---------------------------------------------------------------- Editor <-> model

    private void LoadLetterIntoEditor(CoverLetter letter)
    {
        _letterUndoManager.Clear();
        _letterDraftCts?.Cancel();
        _letterPreviewCts?.Cancel();
        _isLoadingLetter = true;
        try
        {
            CurrentLetter = letter;

            LetterName = letter.Name;
            LetterRecipientName = letter.RecipientName;
            LetterRecipientTitle = letter.RecipientTitle;
            LetterCompanyName = letter.CompanyName;
            LetterCompanyAddress = letter.CompanyAddress;
            LetterDate = new DateTimeOffset(DateTime.SpecifyKind(letter.LetterDate.Date, DateTimeKind.Unspecified), TimeSpan.Zero);
            LetterSubject = letter.Subject;
            LetterSalutation = letter.Salutation;
            LetterClosing = letter.Closing;

            LetterParagraphs = new ObservableCollection<LetterParagraphViewModel>(
                letter.Paragraphs.Select(CreateLetterParagraph));

            UpdateSalutationPlaceholder();
        }
        finally
        {
            _isLoadingLetter = false;
        }

        UpdateLetterPreviewDebounced();
    }

    private void SyncLetterEditorToModel()
    {
        if (_isLoadingLetter) return;

        CurrentLetter.Name = string.IsNullOrWhiteSpace(LetterName) ? DefaultLetterName : LetterName.Trim();
        CurrentLetter.RecipientName = LetterRecipientName;
        CurrentLetter.RecipientTitle = LetterRecipientTitle;
        CurrentLetter.CompanyName = LetterCompanyName;
        CurrentLetter.CompanyAddress = LetterCompanyAddress;
        CurrentLetter.LetterDate = LetterDate.Date;
        CurrentLetter.Subject = LetterSubject;
        CurrentLetter.Salutation = LetterSalutation;
        CurrentLetter.Closing = LetterClosing;

        CurrentLetter.Paragraphs = LetterParagraphs
            .Select(p => p.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();
    }

    private void OnLetterEditorChanged()
    {
        if (_isLoadingLetter) return;

        SyncLetterEditorToModel();
        UpdateSalutationPlaceholder();
        MarkLetterDirty();
        UpdateLetterPreviewDebounced();
    }

    /// <summary>The fallback the letter would use, shown as the watermark on an empty salutation.</summary>
    private void UpdateSalutationPlaceholder() =>
        LetterSalutationPlaceholder = string.IsNullOrWhiteSpace(LetterRecipientName)
            ? "Dear Hiring Manager,"
            : $"Dear {LetterRecipientName.Trim()},";

    partial void OnLetterNameChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterName), oldValue ?? "", newValue, v => LetterName = v); OnLetterEditorChanged(); }
    partial void OnLetterRecipientNameChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterRecipientName), oldValue ?? "", newValue, v => LetterRecipientName = v); OnLetterEditorChanged(); }
    partial void OnLetterRecipientTitleChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterRecipientTitle), oldValue ?? "", newValue, v => LetterRecipientTitle = v); OnLetterEditorChanged(); }
    partial void OnLetterCompanyNameChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterCompanyName), oldValue ?? "", newValue, v => LetterCompanyName = v); OnLetterEditorChanged(); }
    partial void OnLetterCompanyAddressChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterCompanyAddress), oldValue ?? "", newValue, v => LetterCompanyAddress = v); OnLetterEditorChanged(); }
    partial void OnLetterDateChanged(DateTimeOffset oldValue, DateTimeOffset newValue)
    {
        if (!_isLoadingLetter && !_letterUndoManager.IsExecutingAction)
            _letterUndoManager.RecordAction(new PropertyChangeAction<DateTimeOffset>(v => LetterDate = v, oldValue, newValue, "Change letter date"));
        OnLetterEditorChanged();
    }
    partial void OnLetterSubjectChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSubject), oldValue ?? "", newValue, v => LetterSubject = v); OnLetterEditorChanged(); }
    partial void OnLetterSalutationChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSalutation), oldValue ?? "", newValue, v => LetterSalutation = v); OnLetterEditorChanged(); }
    partial void OnLetterClosingChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterClosing), oldValue ?? "", newValue, v => LetterClosing = v); OnLetterEditorChanged(); }

    private void MarkLetterDirty()
    {
        if (_isLoadingLetter) return;

        _letterDraftCts?.Cancel();
        _letterEdits.MarkDirty();
        IsLetterDirty = true;
        LetterSaveStateText = "Unsaved changes";
    }

    // ---------------------------------------------------------------- Paragraphs

    [RelayCommand]
    private void AddLetterParagraph()
    {
        _letterUndoManager.Execute(CollectionChangeAction<LetterParagraphViewModel>.Add(
            LetterParagraphs, CreateLetterParagraph(""), -1, "paragraph", OnLetterEditorChanged));
    }

    [RelayCommand]
    private void RemoveLetterParagraph(LetterParagraphViewModel paragraph)
    {
        var index = LetterParagraphs.IndexOf(paragraph);
        if (index >= 0)
            _letterUndoManager.Execute(CollectionChangeAction<LetterParagraphViewModel>.Remove(
                LetterParagraphs, paragraph, index, "paragraph", OnLetterEditorChanged));
    }

    [RelayCommand]
    private void MoveLetterParagraphUp(LetterParagraphViewModel paragraph) => MoveLetterParagraph(paragraph, -1);

    [RelayCommand]
    private void MoveLetterParagraphDown(LetterParagraphViewModel paragraph) => MoveLetterParagraph(paragraph, 1);

    private void MoveLetterParagraph(LetterParagraphViewModel paragraph, int offset)
    {
        var index = LetterParagraphs.IndexOf(paragraph);
        var target = index + offset;

        if (index < 0 || target < 0 || target >= LetterParagraphs.Count)
            return;

        _letterUndoManager.Execute(CollectionChangeAction<LetterParagraphViewModel>.Move(
            LetterParagraphs, index, target, "paragraph", OnLetterEditorChanged));
    }

    // ---------------------------------------------------------------- AI draft

    [RelayCommand]
    private async Task DraftCoverLetterWithAiAsync()
    {
        if (LetterParagraphs.Any(p => !string.IsNullOrWhiteSpace(p.Text)) &&
            !await Dialogs.ConfirmAsync("Replace letter draft", "Replace this letter's paragraphs with a new draft?", "Replace", "Keep current text"))
            return;

        IsDraftingLetter = true;
        StatusMessage = "Drafting the cover letter...";
        var letter = CurrentLetter;
        _letterDraftCts?.Cancel();
        _letterDraftCts?.Dispose();
        var request = _letterDraftCts = new CancellationTokenSource();

        try
        {
            var source = letter.SourceResumeJson is { } json ? JsonSerializer.Deserialize<Resume>(json)
                : letter.ResumeId is { } id ? await _services.Repository.GetByIdAsync(id) : null;
            if (source is null)
            {
                await Dialogs.ShowErrorAsync("Source resume unavailable", "Create a new letter from the resume you want to use. This letter's source is no longer available.");
                return;
            }
            var targetRole = string.IsNullOrWhiteSpace(letter.TargetRole) ? source.PersonalInfo.JobTitle : letter.TargetRole;

            // Not gated on IsConfigured: without AI the service still returns a structured draft
            // built from the resume itself.
            var result = await _services.CoverLetterService.DraftAsync(
                source, LetterCompanyName, targetRole, letter.JobDescription, request.Token);

            if (request.IsCancellationRequested || !ReferenceEquals(letter, CurrentLetter)) return;

            if (!result.Success || result.Data == null)
            {
                await Dialogs.ShowErrorAsync("Draft failed", result.ErrorMessage ?? "The letter could not be drafted.");
                return;
            }

            var draft = result.Data;

            _letterUndoManager.Execute(new PropertyChangeAction<ObservableCollection<LetterParagraphViewModel>>(
                value => LetterParagraphs = value!, LetterParagraphs,
                new ObservableCollection<LetterParagraphViewModel>(draft.Paragraphs.Select(CreateLetterParagraph)),
                "Replace letter draft", OnLetterEditorChanged));

            if (string.IsNullOrWhiteSpace(LetterSubject))
                LetterSubject = draft.Subject;

            OnLetterEditorChanged();

            StatusMessage = _services.AiService.IsConfigured
                ? "Draft written - review and edit it"
                : "Draft written from your resume (AI is not configured)";
        }
        catch (Exception ex)
        {
            if (request.IsCancellationRequested) return;
            await Dialogs.ShowErrorAsync("Draft failed", ex.Message);
        }
        finally
        {
            if (ReferenceEquals(request, _letterDraftCts)) IsDraftingLetter = false;
        }
    }

    // ---------------------------------------------------------------- Open / save / delete

    [RelayCommand]
    private async Task NewCoverLetterAsync()
    {
        if (!await ConfirmDiscardLetterChangesAsync())
            return;

        SyncEditorToResume();

        var targetRole = string.IsNullOrWhiteSpace(TargetRoleText) ? JobTitle : TargetRoleText;
        var letter = CoverLetter.FromResume(CurrentResume, null, targetRole);

        LoadLetterIntoEditor(letter);

        _letterEdits.MarkDirty();
        IsLetterDirty = true;
        LetterSaveStateText = "Unsaved changes";
        ShowCoverLetterList = false;
        ShowCoverLetterEditor = true;
        StatusMessage = "New cover letter created from this resume";
    }

    [RelayCommand]
    private async Task OpenCoverLetterListAsync()
    {
        await LoadCoverLettersAsync();
        ShowCoverLetterList = true;
    }

    [RelayCommand]
    private void CloseCoverLetterList() => ShowCoverLetterList = false;

    [RelayCommand]
    private async Task CloseCoverLetterEditorAsync()
    {
        if (!await ConfirmDiscardLetterChangesAsync())
            return;

        ShowCoverLetterEditor = false;
    }

    private async Task LoadCoverLettersAsync()
    {
        try
        {
            var letters = await _services.CoverLetterRepository.GetAllAsync();
            CoverLetters.Clear();
            foreach (var letter in letters)
            {
                CoverLetters.Add(letter);
            }
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Could not load your cover letters", ex.Message);
        }
    }

    [RelayCommand]
    private async Task LoadCoverLetterAsync(CoverLetter letter)
    {
        if (!await ConfirmDiscardLetterChangesAsync())
            return;

        try
        {
            var current = CurrentLetter;
            var generation = _letterEdits.BeginSave();
            var loaded = await _services.CoverLetterRepository.GetByIdAsync(letter.Id);
            if (!ReferenceEquals(current, CurrentLetter) || generation != _letterEdits.BeginSave()) return;
            if (loaded == null)
            {
                await Dialogs.ShowErrorAsync("Open failed", $"'{letter.Name}' no longer exists.");
                await LoadCoverLettersAsync();
                return;
            }

            LoadLetterIntoEditor(loaded);

            _letterEdits.Reset();
            IsLetterDirty = false;
            LetterSaveStateText = "No changes";
            ShowCoverLetterList = false;
            ShowCoverLetterEditor = true;
            StatusMessage = $"Opened: {loaded.Name}";
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Open failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveCoverLetterAsync()
    {
        var letter = CurrentLetter;
        await _letterSaveGate.WaitAsync();
        try
        {
            if (!ReferenceEquals(letter, CurrentLetter)) return;
            do
            {
                var token = _letterEdits.BeginSave();
                SyncLetterEditorToModel();

                LetterSaveStateText = "Saving...";

                var snapshot = JsonSerializer.Deserialize<CoverLetter>(JsonSerializer.Serialize(letter))!;
                var saved = snapshot.Id == 0
                    ? await _services.CoverLetterRepository.CreateAsync(snapshot)
                    : await _services.CoverLetterRepository.UpdateAsync(snapshot);
                if (!ReferenceEquals(letter, CurrentLetter)) return;
                letter.Id = saved.Id;
                letter.RowVersion = saved.RowVersion;
                letter.CreatedAt = saved.CreatedAt;
                letter.UpdatedAt = saved.UpdatedAt;

                // Same rule as the resume: an edit typed during the write stays unsaved until the next save.
                IsLetterDirty = !_letterEdits.CompleteSave(token);
            } while (IsLetterDirty);
            LetterSaveStateText = IsLetterDirty ? "Unsaved changes" : $"Saved at {DateTime.Now:HH:mm:ss}";
            StatusMessage = $"Saved: {CurrentLetter.Name}";

            await LoadCoverLettersAsync();
        }
        catch (Exception ex)
        {
            LetterSaveStateText = "Unsaved changes";
            await Dialogs.ShowErrorAsync("Save failed", ex.Message);
        }
        finally { _letterSaveGate.Release(); }
    }

    private async Task DuplicateCoverLetterAsync()
    {
        var name = await Dialogs.PromptAsync("Duplicate cover letter", "Name for the copy:", LetterName);
        if (name is null) return;
        var source = CurrentLetter;
        await _letterSaveGate.WaitAsync();
        try
        {
            if (!ReferenceEquals(source, CurrentLetter)) return;
            var generation = _letterEdits.BeginSave();
            SyncLetterEditorToModel();
            var copy = JsonSerializer.Deserialize<CoverLetter>(JsonSerializer.Serialize(source))!;
            copy.Id = 0;
            copy.RowVersion = Guid.NewGuid();
            copy.Name = string.IsNullOrWhiteSpace(name) ? DefaultLetterName : name.Trim();
            var saved = await _services.CoverLetterRepository.CreateAsync(copy);
            if (ReferenceEquals(source, CurrentLetter) && generation == _letterEdits.BeginSave())
            {
                LoadLetterIntoEditor(saved);
                _letterEdits.Reset();
                IsLetterDirty = false;
                LetterSaveStateText = "No changes";
            }
            await LoadCoverLettersAsync();
            StatusMessage = $"Created copy: {saved.Name}";
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Could not duplicate this letter", ex.Message);
        }
        finally { _letterSaveGate.Release(); }
    }

    [RelayCommand]
    private async Task DeleteCoverLetterAsync(CoverLetter letter)
    {
        var confirmed = await Dialogs.ConfirmAsync(
            "Delete cover letter",
            $"Delete \"{letter.Name}\"? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            await _letterSaveGate.WaitAsync();
            try
            {
                await _services.CoverLetterRepository.DeleteAsync(letter.Id);
                if (CurrentLetter.Id == letter.Id)
                {
                    LoadLetterIntoEditor(new CoverLetter());
                    _letterEdits.Reset();
                    IsLetterDirty = false;
                    LetterSaveStateText = "No changes";
                    ShowCoverLetterEditor = false;
                }
            }
            finally { _letterSaveGate.Release(); }

            await LoadCoverLettersAsync();
            StatusMessage = $"Deleted: {letter.Name}";
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Delete failed", ex.Message);
        }
    }

    private async Task AutoSaveCoverLetterAsync()
    {
        if (IsLoading) return;
        if (!ShowCoverLetterEditor || !IsLetterDirty)
            return;

        // Never let the timer create a record for a letter with no body.
        if (CurrentLetter.Id == 0 && LetterParagraphs.All(p => string.IsNullOrWhiteSpace(p.Text)))
            return;

        await SaveCoverLetterAsync();
    }

    private async Task<bool> ConfirmDiscardLetterChangesAsync()
    {
        await _letterSaveGate.WaitAsync();
        _letterSaveGate.Release();
        if (!IsLetterDirty)
            return true;

        var choice = await Dialogs.ConfirmUnsavedChangesAsync(LetterName);

        return choice switch
        {
            UnsavedChangesChoice.Save => await SaveAndReportAsync(),
            UnsavedChangesChoice.Discard => true,
            _ => false
        };

        async Task<bool> SaveAndReportAsync()
        {
            await SaveCoverLetterAsync();
            return !IsLetterDirty;
        }
    }

    // ---------------------------------------------------------------- Preview / export

    private void UpdateLetterPreviewDebounced()
    {
        if (_letterPreviewTimer == null)
        {
            _letterPreviewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _letterPreviewTimer.Tick += (_, _) =>
            {
                _letterPreviewTimer!.Stop();
                _ = UpdateLetterPreviewAsync();
            };
        }

        _letterPreviewTimer.Stop();
        _letterPreviewTimer.Start();
    }

    private async Task UpdateLetterPreviewAsync()
    {
        if (_services == null) return;

        SyncLetterEditorToModel();

        string json;
        try
        {
            json = JsonSerializer.Serialize(CurrentLetter);
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Preview failed", ex.Message);
            return;
        }

        var templateId = CurrentLetter.SelectedTemplateId;
        var signature = $"{templateId}|{json}";
        if (signature == _letterRenderedSignature)
            return;

        _letterPreviewCts?.Cancel();
        _letterPreviewCts?.Dispose();
        var cts = new CancellationTokenSource();
        _letterPreviewCts = cts;

        try
        {
            // Render off a snapshot: the editor keeps mutating CurrentLetter while this runs.
            var snapshot = JsonSerializer.Deserialize<CoverLetter>(json)!;
            var registry = _services.TemplateRegistry;

            var pages = await Task.Run(() =>
            {
                var template = registry.GetCoverLetterTemplateOrDefault(templateId);
                var document = template.CreateDocument(snapshot);
                return document.GenerateImages(new ImageGenerationSettings
                {
                    ImageFormat = ImageFormat.Png,
                    RasterDpi = 100
                }).ToList();
            }, cts.Token);

            if (cts.IsCancellationRequested)
                return;

            var rendered = new ObservableCollection<Bitmap>();
            foreach (var pageBytes in pages)
            {
                using var stream = new MemoryStream(pageBytes);
                rendered.Add(new Bitmap(stream));
            }

            var superseded = LetterPreviewPages.ToList();

            LetterPreviewPages = rendered;
            _letterRenderedSignature = signature;

            // Dispose only after the bindings have picked up the new pages.
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var bitmap in superseded)
                {
                    bitmap.Dispose();
                }
            }, DispatcherPriority.Background);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Preview failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportCoverLetterAsync(string format)
    {
        try
        {
            var topLevel = MainWindow;
            if (topLevel == null) return;

            SyncLetterEditorToModel();

            var exporter = _services.CoverLetterExportService.GetExporter(format);
            if (exporter == null)
            {
                await Dialogs.ShowErrorAsync("Export failed", $"Unknown export format: {format}");
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = $"Export cover letter as {format}",
                SuggestedFileName = BuildLetterExportFileName(),
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType(format)
                    {
                        Patterns = new[] { $"*{exporter.FileExtension}" }
                    }
                },
                DefaultExtension = exporter.FileExtension.TrimStart('.')
            });

            if (file == null)
                return;

            LoadingMessage = $"Exporting {format}...";
            IsLoading = true;
            var filePath = file.Path.LocalPath;

            // Same as the resume export: rendering is synchronous CPU work, and the overlay only
            // paints if the UI thread is free. Snapshot so the editor can't race the render.
            var snapshot = JsonSerializer.Deserialize<CoverLetter>(JsonSerializer.Serialize(CurrentLetter))!;
            await Task.Run(() => _services.CoverLetterExportService.ExportToFileAsync(snapshot, format, filePath));
            StatusMessage = $"Exported to: {filePath}";
            ShowExportedToast(filePath);
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Export failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string BuildLetterExportFileName()
    {
        var candidate = CurrentLetter.PersonalInfo.FullName;

        if (string.IsNullOrWhiteSpace(candidate))
            candidate = CurrentLetter.Name;

        if (string.IsNullOrWhiteSpace(candidate) || candidate == DefaultLetterName)
            return "Cover_Letter";

        var sanitized = SanitizeFileName(candidate.Trim()).Replace(" ", "_");
        return string.IsNullOrWhiteSpace(sanitized) ? "Cover_Letter" : $"{sanitized}_Cover_Letter";
    }
}
