using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeBuilder.App.Services;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.App.ViewModels;

/// <summary>
/// The application tracker.
///
/// Tailoring already produces a résumé per application and stores the target role and the posting
/// with it, but a list of variants with no company, date, or outcome cannot answer the question
/// that matters when the phone rings: which version did *they* read, and how long have they had it.
/// This is that answer.
/// </summary>
public partial class MainWindowViewModel
{
    [ObservableProperty] private bool _showApplications;

    [ObservableProperty] private string _applicationSummary = "";

    public ObservableCollection<JobApplicationViewModel> Applications { get; } = new();

    /// <summary>Every status, for the per-row picker.</summary>
    public IReadOnlyList<ApplicationStatus> ApplicationStatuses { get; } =
        Enum.GetValues<ApplicationStatus>();

    public bool HasApplications => Applications.Count > 0;

    [RelayCommand]
    private async Task OpenApplicationsAsync()
    {
        if (!await FlushApplicationsAsync())
        {
            ShowApplications = true;
            return;
        }
        await LoadCoverLettersAsync();
        await LoadApplicationsAsync();
        ShowApplications = true;
    }

    [RelayCommand]
    private async Task CloseApplicationsAsync()
    {
        if (await FlushApplicationsAsync())
            ShowApplications = false;
    }

    private async Task LoadApplicationsAsync()
    {
        if (_services?.JobApplicationRepository is not { } repository)
            return;

        try
        {
            var all = await repository.GetAllAsync();

            Applications.Clear();
            foreach (var application in all)
                Applications.Add(new JobApplicationViewModel(application, SaveApplicationAsync, UpdateApplicationSummary, CoverLetters.ToList()));

            OnPropertyChanged(nameof(HasApplications));
            UpdateApplicationSummary();
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Could not load your applications", ex.Message);
        }
    }

    /// <summary>
    /// The one-line read of the whole list. Leads with what is waiting on someone else, because
    /// that is the only part the user can act on today.
    /// </summary>
    private void UpdateApplicationSummary()
    {
        RefreshApplicationFilter();
        if (Applications.Count == 0)
        {
            ApplicationSummary = "";
            return;
        }

        var waiting = Applications.Count(a => a.Status == ApplicationStatus.Applied);
        var stale = Applications.Count(a => a.IsStale);
        var interviewing = Applications.Count(a => a.Status == ApplicationStatus.Interviewing);

        var parts = new List<string> { $"{Applications.Count} tracked" };
        if (Applications.Any(a => a.SaveError != null)) parts.Add("Changes not saved: press Save to retry");
        else if (Applications.Any(a => a.HasPendingChanges)) parts.Add("Saving changes...");
        var due = Applications.Count(a => a.IsFollowUpDue);
        if (due > 0) parts.Add($"{due} follow-up due");
        if (waiting > 0) parts.Add($"{waiting} waiting");
        if (interviewing > 0) parts.Add($"{interviewing} interviewing");
        if (stale > 0) parts.Add($"{stale} silent 2+ weeks");

        ApplicationSummary = string.Join(" · ", parts);
    }

    /// <summary>
    /// Tracks the résumé currently open. Pre-fills the company from nothing but fills the role
    /// from the variant's target, so a tailored résumé needs one field typed rather than three.
    /// </summary>
    [RelayCommand]
    private async Task TrackCurrentResumeAsync()
    {
        if (_services?.JobApplicationRepository is not { } repository)
            return;

        try
        {
            // Save first if this résumé has never been persisted. Tracking an application against
            // an unsaved résumé would record that you applied without recording what you sent,
            // which is the one thing the tracker is for.
            if ((CurrentResume.Id == 0 || IsDirty) && !await SaveAsync(isAutoSave: false))
                return;

            var created = await repository.CreateAsync(new JobApplication
            {
                ResumeId = CurrentResume.Id == 0 ? null : CurrentResume.Id,
                Role = string.IsNullOrWhiteSpace(TargetRoleText) ? JobTitle : TargetRoleText,
                Status = ApplicationStatus.Applied
            });

            Applications.Insert(0, new JobApplicationViewModel(created, SaveApplicationAsync, UpdateApplicationSummary, CoverLetters.ToList()));
            OnPropertyChanged(nameof(HasApplications));
            UpdateApplicationSummary();

            ShowApplications = true;
            StatusMessage = "Tracked - add the company name";
        }
        catch (Exception ex)
        {
            await Dialogs.ShowErrorAsync("Could not track this application", ex.Message);
        }
    }

    /// <summary>
    /// Saves a row as it is edited. A tracker you have to remember to save is one that quietly
    /// goes out of date, which defeats the point.
    /// </summary>
    private Task<JobApplication> SaveApplicationAsync(JobApplication snapshot) =>
        _services.JobApplicationRepository.UpdateAsync(snapshot);

    private async Task<bool> FlushApplicationsAsync(bool showErrors = true)
    {
        foreach (var row in Applications.ToList())
        {
            if (!await row.FlushAsync())
            {
                if (showErrors)
                    await Dialogs.ShowErrorAsync("Application changes not saved", row.SaveError ?? "Try saving again before continuing.");
                return false;
            }
        }
        return true;
    }

    [RelayCommand]
    private async Task DeleteApplicationAsync(JobApplicationViewModel? row)
    {
        if (row is null || _services?.JobApplicationRepository is not { } repository)
            return;

        var confirmed = await Dialogs.ConfirmAsync(
            "Remove application",
            $"Stop tracking {(string.IsNullOrWhiteSpace(row.Company) ? "this application" : row.Company)}? " +
            "The résumé itself is not deleted.",
            "Remove");

        if (!confirmed)
            return;

        await row.StopSavingAsync();
        try
        {
            await repository.DeleteAsync(row.Id);
        }
        catch (Exception ex)
        {
            row.ResumeSaving();
            await Dialogs.ShowErrorAsync("Could not remove this application", ex.Message);
            return;
        }
        Applications.Remove(row);
        OnPropertyChanged(nameof(HasApplications));
        UpdateApplicationSummary();
    }

    /// <summary>Opens the résumé that was actually sent: the reason the tracker exists.</summary>
    [RelayCommand]
    private async Task OpenApplicationResumeAsync(JobApplicationViewModel? row)
    {
        if (row is null)
            return;

        if (_services?.Repository is not { } repository)
            return;

        if (!await row.FlushAsync())
            return;

        if (row.SentResumeJson is { } sentJson)
        {
            if (!await ConfirmDiscardChangesAsync()) return;
            var snapshot = JsonSerializer.Deserialize<Resume>(sentJson)!;
            AdoptImportedResume(snapshot);
            ShowApplications = false;
            StatusMessage = "Opened the sent version. Edits will save as a separate copy.";
            return;
        }

        var resume = row.ResumeId is { } resumeId ? await repository.GetByIdAsync(resumeId) : null;
        if (resume is null)
        {
            StatusMessage = "That resume has since been deleted";
            return;
        }

        ShowApplications = false;
        await LoadResumeAsync(resume);
        StatusMessage = "Opened linked resume. This historical application has no saved sent version.";
    }
}
