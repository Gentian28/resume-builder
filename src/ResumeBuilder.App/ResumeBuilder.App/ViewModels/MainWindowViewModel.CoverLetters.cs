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

namespace ResumeBuilder.App.ViewModels;

public partial class MainWindowViewModel
{
    private const string DefaultLetterName = "Untitled Cover Letter";

    private DispatcherTimer? _letterPreviewTimer;
    private CancellationTokenSource? _letterPreviewCts;
    private string? _letterRenderedSignature;
    private bool _isLoadingLetter;

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
        _isLoadingLetter = true;
        try
        {
            CurrentLetter = letter;

            LetterName = letter.Name;
            LetterRecipientName = letter.RecipientName;
            LetterRecipientTitle = letter.RecipientTitle;
            LetterCompanyName = letter.CompanyName;
            LetterCompanyAddress = letter.CompanyAddress;
            LetterDate = new DateTimeOffset(letter.LetterDate.Date, TimeSpan.Zero);
            LetterSubject = letter.Subject;
            LetterSalutation = letter.Salutation;
            LetterClosing = letter.Closing;

            LetterParagraphs = new ObservableCollection<LetterParagraphViewModel>(
                letter.Paragraphs.Select(p => new LetterParagraphViewModel(p, OnLetterEditorChanged)));

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

    partial void OnLetterNameChanged(string value) => OnLetterEditorChanged();
    partial void OnLetterRecipientNameChanged(string value) => OnLetterEditorChanged();
    partial void OnLetterRecipientTitleChanged(string value) => OnLetterEditorChanged();
    partial void OnLetterCompanyNameChanged(string value) => OnLetterEditorChanged();
    partial void OnLetterCompanyAddressChanged(string value) => OnLetterEditorChanged();
    partial void OnLetterDateChanged(DateTimeOffset value) => OnLetterEditorChanged();
    partial void OnLetterSubjectChanged(string value) => OnLetterEditorChanged();
    partial void OnLetterSalutationChanged(string value) => OnLetterEditorChanged();
    partial void OnLetterClosingChanged(string value) => OnLetterEditorChanged();

    private void MarkLetterDirty()
    {
        if (_isLoadingLetter) return;

        IsLetterDirty = true;
        LetterSaveStateText = "Unsaved changes";
    }

    // ---------------------------------------------------------------- Paragraphs

    [RelayCommand]
    private void AddLetterParagraph()
    {
        LetterParagraphs.Add(new LetterParagraphViewModel("", OnLetterEditorChanged));
        OnLetterEditorChanged();
    }

    [RelayCommand]
    private void RemoveLetterParagraph(LetterParagraphViewModel paragraph)
    {
        LetterParagraphs.Remove(paragraph);
        OnLetterEditorChanged();
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

        LetterParagraphs.Move(index, target);
        OnLetterEditorChanged();
    }

    // ---------------------------------------------------------------- AI draft

    [RelayCommand]
    private async Task DraftCoverLetterWithAiAsync()
    {
        SyncEditorToResume();

        IsDraftingLetter = true;
        StatusMessage = "Drafting the cover letter...";

        try
        {
            var targetRole = string.IsNullOrWhiteSpace(TargetRoleText) ? JobTitle : TargetRoleText;

            // Not gated on IsConfigured: without AI the service still returns a structured draft
            // built from the resume itself.
            var result = await _services.CoverLetterService.DraftAsync(
                CurrentResume, LetterCompanyName, targetRole, JobDescriptionText);

            if (!result.Success || result.Data == null)
            {
                await DialogService.ShowErrorAsync("Draft failed", result.ErrorMessage ?? "The letter could not be drafted.");
                return;
            }

            var draft = result.Data;

            LetterParagraphs = new ObservableCollection<LetterParagraphViewModel>(
                draft.Paragraphs.Select(p => new LetterParagraphViewModel(p, OnLetterEditorChanged)));

            if (string.IsNullOrWhiteSpace(LetterSubject))
                LetterSubject = draft.Subject;

            OnLetterEditorChanged();

            StatusMessage = _services.AiService.IsConfigured
                ? "Draft written - review and edit it"
                : "Draft written from your resume (AI is not configured)";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Draft failed", ex.Message);
        }
        finally
        {
            IsDraftingLetter = false;
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
            await DialogService.ShowErrorAsync("Could not load your cover letters", ex.Message);
        }
    }

    [RelayCommand]
    private async Task LoadCoverLetterAsync(CoverLetter letter)
    {
        if (!await ConfirmDiscardLetterChangesAsync())
            return;

        try
        {
            var loaded = await _services.CoverLetterRepository.GetByIdAsync(letter.Id);
            if (loaded == null)
            {
                await DialogService.ShowErrorAsync("Open failed", $"'{letter.Name}' no longer exists.");
                await LoadCoverLettersAsync();
                return;
            }

            LoadLetterIntoEditor(loaded);

            IsLetterDirty = false;
            LetterSaveStateText = "No changes";
            ShowCoverLetterList = false;
            ShowCoverLetterEditor = true;
            StatusMessage = $"Opened: {loaded.Name}";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Open failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveCoverLetterAsync()
    {
        try
        {
            SyncLetterEditorToModel();

            LetterSaveStateText = "Saving...";

            if (CurrentLetter.Id == 0)
            {
                await _services.CoverLetterRepository.CreateAsync(CurrentLetter);
            }
            else
            {
                await _services.CoverLetterRepository.UpdateAsync(CurrentLetter);
            }

            IsLetterDirty = false;
            LetterSaveStateText = $"Saved at {DateTime.Now:HH:mm:ss}";
            StatusMessage = $"Saved: {CurrentLetter.Name}";

            await LoadCoverLettersAsync();
        }
        catch (Exception ex)
        {
            LetterSaveStateText = "Unsaved changes";
            await DialogService.ShowErrorAsync("Save failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteCoverLetterAsync(CoverLetter letter)
    {
        var confirmed = await DialogService.ConfirmAsync(
            "Delete cover letter",
            $"Delete \"{letter.Name}\"? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            await _services.CoverLetterRepository.DeleteAsync(letter.Id);

            // The editor is showing the row that just went away; keep the content, drop the identity.
            if (CurrentLetter.Id == letter.Id)
            {
                CurrentLetter.Id = 0;
                MarkLetterDirty();
            }

            await LoadCoverLettersAsync();
            StatusMessage = $"Deleted: {letter.Name}";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Delete failed", ex.Message);
        }
    }

    private async Task AutoSaveCoverLetterAsync()
    {
        if (!ShowCoverLetterEditor || !IsLetterDirty)
            return;

        // Never let the timer create a record for a letter with no body.
        if (CurrentLetter.Id == 0 && LetterParagraphs.All(p => string.IsNullOrWhiteSpace(p.Text)))
            return;

        await SaveCoverLetterAsync();
    }

    private async Task<bool> ConfirmDiscardLetterChangesAsync()
    {
        if (!IsLetterDirty)
            return true;

        var choice = await DialogService.ConfirmUnsavedChangesAsync(LetterName);

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
            await DialogService.ShowErrorAsync("Preview failed", ex.Message);
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
            await DialogService.ShowErrorAsync("Preview failed", ex.Message);
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
                await DialogService.ShowErrorAsync("Export failed", $"Unknown export format: {format}");
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

            IsLoading = true;
            var filePath = file.Path.LocalPath;

            await _services.CoverLetterExportService.ExportToFileAsync(CurrentLetter, format, filePath);
            StatusMessage = $"Exported to: {filePath}";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Export failed", ex.Message);
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
