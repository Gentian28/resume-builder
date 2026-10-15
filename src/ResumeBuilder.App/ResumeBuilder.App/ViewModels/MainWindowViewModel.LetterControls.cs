using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.UndoRedo;
using ResumeBuilder.Data;

namespace ResumeBuilder.App.ViewModels;

public partial class MainWindowViewModel
{
    [ObservableProperty] private TemplateInfo? _selectedLetterTemplate;
    [ObservableProperty] private PaperSize _letterPaperSize;
    [ObservableProperty] private DocumentLanguage _letterDocumentLanguage;
    [ObservableProperty] private string _letterSourceLabel = "Source unavailable";
    [ObservableProperty] private string _letterTargetRole = "";
    [ObservableProperty] private string _letterJobDescription = "";
    private IReadOnlyList<TemplateInfo>? _letterTemplates;
    public IReadOnlyList<TemplateInfo> LetterTemplates => _letterTemplates ??= _services.TemplateRegistry.GetAllCoverLetterTemplates().Select(t => t.Info).ToList();
    partial void OnSelectedLetterTemplateChanged(TemplateInfo? oldValue, TemplateInfo? newValue)
    {
        if (_isLoadingLetter || newValue is null) return;
        CurrentLetter.SelectedTemplateId = newValue.Id;
        if (oldValue != null && !_letterUndoManager.IsExecutingAction) _letterUndoManager.RecordAction(new PropertyChangeAction<TemplateInfo>(v => SelectedLetterTemplate = v, oldValue, newValue, "Change letter template"));
        OnLetterEditorChanged();
    }
    partial void OnLetterPaperSizeChanged(PaperSize oldValue, PaperSize newValue)
    {
        if (_isLoadingLetter) return;
        CurrentLetter.TemplateSettings.PaperSize = newValue;
        if (!_letterUndoManager.IsExecutingAction) _letterUndoManager.RecordAction(new PropertyChangeAction<PaperSize>(v => LetterPaperSize = v, oldValue, newValue, "Change letter paper"));
        OnLetterEditorChanged();
    }
    partial void OnLetterDocumentLanguageChanged(DocumentLanguage oldValue, DocumentLanguage newValue)
    {
        if (_isLoadingLetter) return;
        CurrentLetter.TemplateSettings.DocumentLanguage = newValue;
        if (!_letterUndoManager.IsExecutingAction) _letterUndoManager.RecordAction(new PropertyChangeAction<DocumentLanguage>(v => LetterDocumentLanguage = v, oldValue, newValue, "Change letter language"));
        OnLetterEditorChanged();
    }
    partial void OnLetterTargetRoleChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterTargetRole), oldValue ?? "", newValue, v => LetterTargetRole = v); OnLetterEditorChanged(); }
    partial void OnLetterJobDescriptionChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterJobDescription), oldValue ?? "", newValue, v => LetterJobDescription = v); OnLetterEditorChanged(); }
    [ObservableProperty] private string _letterSenderFirstName = "";
    partial void OnLetterSenderFirstNameChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSenderFirstName), oldValue ?? "", newValue, v => LetterSenderFirstName = v); OnLetterEditorChanged(); }
    [ObservableProperty] private string _letterSenderLastName = "";
    partial void OnLetterSenderLastNameChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSenderLastName), oldValue ?? "", newValue, v => LetterSenderLastName = v); OnLetterEditorChanged(); }
    [ObservableProperty] private string _letterSenderEmail = "";
    partial void OnLetterSenderEmailChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSenderEmail), oldValue ?? "", newValue, v => LetterSenderEmail = v); OnLetterEditorChanged(); }
    [ObservableProperty] private string _letterSenderPhone = "";
    partial void OnLetterSenderPhoneChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSenderPhone), oldValue ?? "", newValue, v => LetterSenderPhone = v); OnLetterEditorChanged(); }
    [ObservableProperty] private string _letterSenderAddress = "";
    partial void OnLetterSenderAddressChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSenderAddress), oldValue ?? "", newValue, v => LetterSenderAddress = v); OnLetterEditorChanged(); }
    [ObservableProperty] private string _letterSenderCity = "";
    partial void OnLetterSenderCityChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSenderCity), oldValue ?? "", newValue, v => LetterSenderCity = v); OnLetterEditorChanged(); }
    [ObservableProperty] private string _letterSenderCountry = "";
    partial void OnLetterSenderCountryChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSenderCountry), oldValue ?? "", newValue, v => LetterSenderCountry = v); OnLetterEditorChanged(); }
    [ObservableProperty] private string _letterSenderWebsite = "";
    partial void OnLetterSenderWebsiteChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSenderWebsite), oldValue ?? "", newValue, v => LetterSenderWebsite = v); OnLetterEditorChanged(); }
    [ObservableProperty] private string _letterSenderLinkedIn = "";
    partial void OnLetterSenderLinkedInChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSenderLinkedIn), oldValue ?? "", newValue, v => LetterSenderLinkedIn = v); OnLetterEditorChanged(); }
    [ObservableProperty] private string _letterSenderGitHub = "";
    partial void OnLetterSenderGitHubChanged(string? oldValue, string newValue) { RecordLetterEdit(nameof(LetterSenderGitHub), oldValue ?? "", newValue, v => LetterSenderGitHub = v); OnLetterEditorChanged(); }
    private void LoadLetterControls()
    {
        SelectedLetterTemplate = LetterTemplates.FirstOrDefault(t => t.Id == CurrentLetter.SelectedTemplateId) ?? LetterTemplates.First();
        LetterPaperSize = CurrentLetter.TemplateSettings.PaperSize;
        LetterDocumentLanguage = CurrentLetter.TemplateSettings.DocumentLanguage;
        LetterTargetRole = CurrentLetter.TargetRole;
        LetterJobDescription = CurrentLetter.JobDescription;
        try
        {
            var source = CurrentLetter.SourceResumeJson is { } json ? JsonSerializer.Deserialize<Resume>(json) : null;
            LetterSourceLabel = source != null ? $"Source: {source.Name} (captured copy)" : "Historical source: linked resume";
        }
        catch (JsonException) { LetterSourceLabel = "Source unavailable"; }
        LetterSenderFirstName = CurrentLetter.PersonalInfo.FirstName;
        LetterSenderLastName = CurrentLetter.PersonalInfo.LastName;
        LetterSenderEmail = CurrentLetter.PersonalInfo.Email;
        LetterSenderPhone = CurrentLetter.PersonalInfo.Phone;
        LetterSenderAddress = CurrentLetter.PersonalInfo.Address;
        LetterSenderCity = CurrentLetter.PersonalInfo.City;
        LetterSenderCountry = CurrentLetter.PersonalInfo.Country;
        LetterSenderWebsite = CurrentLetter.PersonalInfo.Website;
        LetterSenderLinkedIn = CurrentLetter.PersonalInfo.LinkedIn;
        LetterSenderGitHub = CurrentLetter.PersonalInfo.GitHub;
    }
    private void SyncLetterControls()
    {
        CurrentLetter.TargetRole = LetterTargetRole;
        CurrentLetter.JobDescription = LetterJobDescription;
        CurrentLetter.PersonalInfo.FirstName = LetterSenderFirstName;
        CurrentLetter.PersonalInfo.LastName = LetterSenderLastName;
        CurrentLetter.PersonalInfo.Email = LetterSenderEmail;
        CurrentLetter.PersonalInfo.Phone = LetterSenderPhone;
        CurrentLetter.PersonalInfo.Address = LetterSenderAddress;
        CurrentLetter.PersonalInfo.City = LetterSenderCity;
        CurrentLetter.PersonalInfo.Country = LetterSenderCountry;
        CurrentLetter.PersonalInfo.Website = LetterSenderWebsite;
        CurrentLetter.PersonalInfo.LinkedIn = LetterSenderLinkedIn;
        CurrentLetter.PersonalInfo.GitHub = LetterSenderGitHub;
    }
    [RelayCommand]
    private async Task UseOpenResumeForLetterAsync()
    {
        if (!await Dialogs.ConfirmAsync("Change letter source", "Use the open resume as this letter's source, sender and styling? Existing body text stays and should be reviewed.", "Use open resume", "Cancel")) return;
        SyncEditorToResume(); SyncLetterEditorToModel();
        var old = JsonSerializer.Serialize(new LetterSourceState(CurrentLetter));
        var replacement = CoverLetter.FromResume(ResumeRepository.DeepClone(CurrentResume), CurrentLetter.CompanyName, TargetRoleText);
        var next = JsonSerializer.Serialize(new LetterSourceState(replacement));
        _letterUndoManager.Execute(new TextEditAction("LetterSource", old, next, ApplyLetterSource, "Change letter source"));
    }
    private void ApplyLetterSource(string json)
    {
        var state = JsonSerializer.Deserialize<LetterSourceState>(json)!;
        _isLoadingLetter = true;
        try
        {
            CurrentLetter.ResumeId = state.ResumeId; CurrentLetter.SourceResumeJson = state.SourceResumeJson;
            CurrentLetter.PersonalInfo = state.PersonalInfo; CurrentLetter.TemplateSettings = state.TemplateSettings;
            CurrentLetter.TargetRole = state.TargetRole; CurrentLetter.JobDescription = state.JobDescription;
            LoadLetterControls();
        }
        finally { _isLoadingLetter = false; }
        OnLetterEditorChanged();
    }
    public sealed class LetterSourceState
    {
        public LetterSourceState() { }
        public LetterSourceState(CoverLetter letter)
        {
            ResumeId = letter.ResumeId; SourceResumeJson = letter.SourceResumeJson; PersonalInfo = letter.PersonalInfo;
            TemplateSettings = letter.TemplateSettings; TargetRole = letter.TargetRole; JobDescription = letter.JobDescription;
        }
        public int? ResumeId { get; set; }
        public string? SourceResumeJson { get; set; }
        public PersonalInfo PersonalInfo { get; set; } = new();
        public TemplateSettings TemplateSettings { get; set; } = new();
        public string TargetRole { get; set; } = "";
        public string JobDescription { get; set; } = "";
    }
}
