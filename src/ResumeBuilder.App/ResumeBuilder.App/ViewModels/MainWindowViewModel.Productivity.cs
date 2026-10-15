using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeBuilder.Core.Editing;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.Sync;
using ResumeBuilder.Core.Validation;
using ResumeBuilder.Data;
using UglyToad.PdfPig;

namespace ResumeBuilder.App.ViewModels;

public partial class MainWindowViewModel
{
    [ObservableProperty] private string _resumeSearch = "";
    [ObservableProperty] private string _applicationSearch = "";
    [ObservableProperty] private string _applicationFilter = "All";
    [ObservableProperty] private bool _showReview;
    [ObservableProperty] private string _reviewTitle = "";
    [ObservableProperty] private string _reviewText = "";
    [ObservableProperty] private string _importReviewText = "";
    [ObservableProperty] private PaperSize _selectedPaperSize;
    [ObservableProperty] private DocumentLanguage _selectedDocumentLanguage;
    public bool HasImportReview => !string.IsNullOrEmpty(ImportReviewText);
    public PaperSize[] PaperSizes => Enum.GetValues<PaperSize>();
    public DocumentLanguage[] DocumentLanguages => Enum.GetValues<DocumentLanguage>();
    public string[] ApplicationFilters => ["All", "Active", "Archived", "Follow-up due"];
    public string PreviewPageCount => $"{PreviewPages.Count} page{(PreviewPages.Count == 1 ? "" : "s")}";
    public IEnumerable<Resume> FilteredResumes
    {
        get
        {
            bool Matches(Resume r) => string.Join(" ", r.Name, r.PersonalInfo.FullName, r.TargetRole).Contains(ResumeSearch.Trim(), StringComparison.OrdinalIgnoreCase);
            var matches = SavedResumes.Where(Matches).ToList();
            var parentIds = matches.Select(r => r.BaseResumeId).ToHashSet();
            var baseIds = matches.Where(r => !r.IsVariant).Select(r => r.Id).ToHashSet();
            return SavedResumes.Where(r => matches.Contains(r) || parentIds.Contains(r.Id) || r.BaseResumeId is { } id && baseIds.Contains(id)).ToList();
        }
    }
    public IEnumerable<JobApplicationViewModel> FilteredApplications => Applications.Where(r =>
        string.Join(" ", r.Company, r.Role, r.Contact).Contains(ApplicationSearch.Trim(), StringComparison.OrdinalIgnoreCase) &&
        (ApplicationFilter switch
        {
            "Active" => !ApplicationTools.IsArchived(r.Status),
            "Archived" => ApplicationTools.IsArchived(r.Status),
            "Follow-up due" => r.IsFollowUpDue,
            _ => true
        })).ToList();
    public bool HasResumeMatches => FilteredResumes.Any();
    public bool HasApplicationMatches => FilteredApplications.Any();
    partial void OnResumeSearchChanged(string value) => RefreshResumeFilter();
    partial void OnApplicationSearchChanged(string value) => RefreshApplicationFilter();
    partial void OnApplicationFilterChanged(string value) => RefreshApplicationFilter();
    partial void OnImportReviewTextChanged(string value) => OnPropertyChanged(nameof(HasImportReview));
    private void RefreshResumeFilter() { OnPropertyChanged(nameof(FilteredResumes)); OnPropertyChanged(nameof(HasResumeMatches)); }
    private void RefreshApplicationFilter() { OnPropertyChanged(nameof(FilteredApplications)); OnPropertyChanged(nameof(HasApplicationMatches)); }
    partial void OnPreviewPagesChanged(ObservableCollection<Avalonia.Media.Imaging.Bitmap> value) => OnPropertyChanged(nameof(PreviewPageCount));
    partial void OnSelectedPaperSizeChanged(PaperSize oldValue, PaperSize newValue)
    {
        if (_isLoadingEditor) return;
        CurrentResume.TemplateSettings.PaperSize = newValue;
        RecordTextEdit("PaperSize", oldValue.ToString(), newValue.ToString(), v => SelectedPaperSize = Enum.Parse<PaperSize>(v), "Change paper size");
        OnEditorChanged();
    }
    partial void OnSelectedDocumentLanguageChanged(DocumentLanguage oldValue, DocumentLanguage newValue)
    {
        if (_isLoadingEditor) return;
        CurrentResume.TemplateSettings.DocumentLanguage = newValue;
        RecordTextEdit("DocumentLanguage", oldValue.ToString(), newValue.ToString(), v => SelectedDocumentLanguage = Enum.Parse<DocumentLanguage>(v), "Change document language");
        OnEditorChanged();
    }

    [RelayCommand]
    private async Task OpenExampleAsync()
    {
        if (!await ConfirmDiscardChangesAsync()) return;
        using var input = typeof(MainWindowViewModel).Assembly.GetManifestResourceStream("ResumeBuilder.App.sample-resume.json")!;
        var result = await _services.ExportService.ImportAsync(input, "JSON");
        if (!result.Success || result.Data is null) { await Dialogs.ShowErrorAsync("Example unavailable", result.ErrorMessage ?? "The bundled example could not be read."); return; }
        result.Data.Name = "Example resume (synthetic)";
        AdoptImportedResume(result.Data);
        SetImportReview(result.Data, ["This is a synthetic example. Replace its details with your own or create a new resume."]);
        StatusMessage = "Example opened. Edit a field, then export PDF.";
    }
    private void SetImportReview(Resume resume, IEnumerable<string> warnings)
    {
        ImportReviewText = $"Review imported content: {resume.Experiences.Count} roles, {resume.EducationList.Count} education entries, {resume.Skills.Count} skills.\n" +
            string.Join("\n", warnings.DefaultIfEmpty("Check contact details, dates and each section against the original."));
    }
    [RelayCommand] private void DismissImportReview() => ImportReviewText = "";
    [RelayCommand] private void CloseReview() => ShowReview = false;
    [RelayCommand]
    private async Task CloseTopOverlayAsync()
    {
        if (ShowReview) ShowReview = false;
        else if (ShowResumeList) ShowResumeList = false;
        else if (ShowCoverLetterList) ShowCoverLetterList = false;
        else if (ShowCoverLetterEditor) await CloseCoverLetterEditorAsync();
        else if (ShowApplications) await CloseApplicationsAsync();
        else if (ShowTemplateGallery) ShowTemplateGallery = false;
        else { ShowAiPanel = false; ShowTailorPanel = false; ShowSyncPanel = false; }
    }
    [RelayCommand]
    private async Task CopyReviewAsync()
    {
        if (MainWindow?.Clipboard is { } clipboard) { await clipboard.SetTextAsync(ReviewText); StatusMessage = "Copied to clipboard"; }
    }
    [RelayCommand]
    private void ShowSupportDiagnostics()
    {
        ReviewTitle = "Support diagnostics: review before copying";
        ReviewText = $"ResumeBuilder {typeof(MainWindowViewModel).Assembly.GetName().Version}\nOS: {RuntimeInformation.OSDescription}\nArchitecture: {RuntimeInformation.OSArchitecture}\nRuntime: {RuntimeInformation.FrameworkDescription}\n" +
            $"Recent error codes: {string.Join(", ", ResumeBuilder.App.Services.DiagnosticDialogs.Codes)}\n" +
            "Document content, personal details, paths, endpoints, keys and raw error messages are excluded.\nDescribe your steps separately with a synthetic example.";
        ShowReview = true;
    }
    [RelayCommand]
    private async Task ReviewExportAsync()
    {
        SyncEditorToResume();
        var snapshot = ResumeRepository.DeepClone(CurrentResume);
        try
        {
            LoadingMessage = "Checking exported PDF..."; IsLoading = true;
            var report = await Task.Run(async () =>
            {
                var bytes = await _services.ExportService.ExportAsync(snapshot, "PDF");
                using var pdf = PdfDocument.Open(bytes);
                var text = string.Join("\n\n", pdf.GetPages().Select(p => p.Text));
                var validation = new ResumeValidator().Validate(snapshot);
                var warnings = validation.Errors.Concat(validation.Warnings).Select(w => $"{w.FieldName}: {w.ErrorMessage}").ToList();
                if (string.IsNullOrWhiteSpace(snapshot.PersonalInfo.FullName)) warnings.Add("Add your name.");
                if (string.IsNullOrWhiteSpace(snapshot.PersonalInfo.Email) && string.IsNullOrWhiteSpace(snapshot.PersonalInfo.Phone)) warnings.Add("Add an email address or phone number.");
                if (snapshot.TemplateSettings.FontSizeScale < 0.9f) warnings.Add("Text is small. Check readability at actual print size.");
                if (string.IsNullOrWhiteSpace(text)) warnings.Add("No extractable text was found in this PDF.");
                return $"{pdf.NumberOfPages} page(s), {snapshot.TemplateSettings.PaperSize}, {snapshot.TemplateSettings.DocumentLanguage}\n\n" +
                    string.Join("\n", warnings.DefaultIfEmpty("No basic readiness warnings found. Review the preview and text yourself before sending.")) +
                    "\n\nThese checks do not predict hiring outcomes or ATS acceptance.\n\nExtracted PDF text\n\n" + text;
            });
            ReviewTitle = "Export readiness and extracted text"; ReviewText = report; ShowReview = true;
        }
        catch (Exception ex) { await Dialogs.ShowErrorAsync("Export review failed", ex.Message); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExportApplicationsAsync(string format)
    {
        if (!await FlushApplicationsAsync() || MainWindow is null) return;
        try
        {
            var extension = format == "calendar" ? "ics" : "csv";
            var file = await MainWindow.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            { Title = $"Export applications as {extension}", SuggestedFileName = $"Applications.{extension}", DefaultExtension = extension });
            if (file is null) return;
            var rows = FilteredApplications.Select(r => r.ToModel()).ToList();
            await AtomicFile.WriteAllTextAsync(file.Path.LocalPath, format == "calendar" ? ApplicationTools.Calendar(rows) : ApplicationTools.Csv(rows));
            StatusMessage = $"Exported {rows.Count} application(s)"; ShowExportedToast(file.Path.LocalPath);
        }
        catch (Exception ex) { await Dialogs.ShowErrorAsync("Export failed", ex.Message); }
    }
    [RelayCommand] private Task ExportSentResumeAsync(JobApplicationViewModel? row) => ExportSentPdfAsync(row?.SentResumePdf, "Sent_resume.pdf");
    [RelayCommand] private Task ExportSentLetterAsync(JobApplicationViewModel? row) => ExportSentPdfAsync(row?.SentCoverLetterPdf, "Sent_letter.pdf");
    private async Task ExportSentPdfAsync(byte[]? bytes, string name)
    {
        if (bytes is null || MainWindow is null) return;
        try
        {
            var file = await MainWindow.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            { Title = "Save captured sent PDF", SuggestedFileName = name, DefaultExtension = "pdf" });
            if (file is null) return;
            await AtomicFile.WriteAllBytesAsync(file.Path.LocalPath, bytes); ShowExportedToast(file.Path.LocalPath);
        }
        catch (Exception ex) { await Dialogs.ShowErrorAsync("Export failed", ex.Message); }
    }
    [RelayCommand]
    private async Task AddDraftApplicationAsync()
    {
        if ((CurrentResume.Id == 0 || IsDirty) && !await SaveAsync(false)) return;
        try
        {
            var row = await _services.JobApplicationRepository.CreateAsync(new JobApplication { ResumeId = CurrentResume.Id, Role = JobTitle, Status = ApplicationStatus.Saved });
            Applications.Insert(0, new(row, SaveApplicationAsync, UpdateApplicationSummary, CoverLetters.ToList()));
            OnPropertyChanged(nameof(HasApplications)); UpdateApplicationSummary();
        }
        catch (Exception ex) { await Dialogs.ShowErrorAsync("Could not track this application", ex.Message); }
    }
    [RelayCommand]
    private async Task OpenApplicationLetterAsync(JobApplicationViewModel? row)
    {
        if (row is null || !await row.FlushAsync() || !await ConfirmDiscardLetterChangesAsync()) return;
        var letter = row.SentCoverLetterJson is { } json ? JsonSerializer.Deserialize<CoverLetter>(json)
            : row.CoverLetterId is { } id ? await _services.CoverLetterRepository.GetByIdAsync(id) : null;
        if (letter is null) { StatusMessage = "No linked letter is available"; return; }
        if (row.SentCoverLetterJson != null) { letter.Id = 0; letter.SyncId = Guid.NewGuid(); letter.RowVersion = Guid.NewGuid(); }
        LoadLetterIntoEditor(letter); _letterEdits.Reset(); IsLetterDirty = row.SentCoverLetterJson != null;
        if (IsLetterDirty) _letterEdits.MarkDirty();
        ShowApplications = false; ShowCoverLetterEditor = true;
        StatusMessage = row.SentCoverLetterJson != null ? "Opened sent letter as a separate copy" : "Opened linked letter. No sent letter version was captured.";
    }
}
