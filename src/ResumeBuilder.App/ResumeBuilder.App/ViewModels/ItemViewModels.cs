using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.SmartContent;
using ResumeBuilder.Core.SpellCheck;

namespace ResumeBuilder.App.ViewModels;

/// <summary>
/// Lets an item view model push a text edit onto the shared undo history without knowing about the
/// undo manager. Implemented by <see cref="MainWindowViewModel"/>.
/// </summary>
public interface ITextEditRecorder
{
    void RecordTextEdit(string fieldKey, string oldValue, string newValue, Action<string> setter, string description);
}

/// <summary>Month/year pickers shared by every dated section.</summary>
public static class DateOptions
{
    public static string[] Months { get; } =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    public static int[] Years { get; } =
        Enumerable.Range(1970, DateTime.Now.Year - 1970 + 10).Reverse().ToArray();

    public static string? ToMonthName(DateTime? date) =>
        date.HasValue ? Months[date.Value.Month - 1] : null;

    /// <summary>
    /// A year on its own is still a date (defaulted to January), so picking a year without a month
    /// does not silently discard the year on save.
    /// </summary>
    public static DateTime? ToDate(int? year, string? monthName)
    {
        if (!year.HasValue)
            return null;

        var month = monthName == null ? -1 : Array.IndexOf(Months, monthName);
        return new DateTime(year.Value, month >= 0 ? month + 1 : 1, 1);
    }
}

public abstract class ItemViewModelBase : ObservableObject
{
    // Stable for the lifetime of the item, so consecutive keystrokes in one field merge into a
    // single undo entry while edits to a different item or field never merge into each other.
    private readonly string _instanceKey = Guid.NewGuid().ToString("N");

    protected ItemViewModelBase(Action? onChanged, ITextEditRecorder? recorder)
    {
        OnChanged = onChanged;
        Recorder = recorder;
    }

    protected Action? OnChanged { get; }
    protected ITextEditRecorder? Recorder { get; }

    protected void TextChanged(string field, string? oldValue, string newValue, Action<string> setter)
    {
        Recorder?.RecordTextEdit(
            $"{GetType().Name}.{_instanceKey}.{field}",
            oldValue ?? string.Empty,
            newValue,
            setter,
            $"Edit {field}");

        OnChanged?.Invoke();
    }

    protected void ValueChanged() => OnChanged?.Invoke();
}

public partial class ExperienceViewModel : ItemViewModelBase
{
    [ObservableProperty] private string _jobTitle = "";
    [ObservableProperty] private string _company = "";
    [ObservableProperty] private string _location = "";
    [ObservableProperty] private string? _startMonthName;
    [ObservableProperty] private int? _startYear;
    [ObservableProperty] private string? _endMonthName;
    [ObservableProperty] private int? _endYear;
    [ObservableProperty] private bool _isCurrentRole;
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _achievements = "";

    public ExperienceViewModel(Experience exp, Action? onChanged = null, ITextEditRecorder? recorder = null)
        : base(onChanged, recorder)
    {
        JobTitle = exp.JobTitle;
        Company = exp.Company;
        Location = exp.Location;
        StartMonthName = DateOptions.ToMonthName(exp.StartDate);
        StartYear = exp.StartDate?.Year;
        EndMonthName = DateOptions.ToMonthName(exp.EndDate);
        EndYear = exp.EndDate?.Year;
        IsCurrentRole = exp.IsCurrentRole;
        Description = exp.Description;
        Achievements = AchievementLines.Format(exp.Achievements);
    }

    partial void OnJobTitleChanged(string? oldValue, string newValue) => TextChanged(nameof(JobTitle), oldValue, newValue, v => JobTitle = v);
    partial void OnCompanyChanged(string? oldValue, string newValue) => TextChanged(nameof(Company), oldValue, newValue, v => Company = v);
    partial void OnLocationChanged(string? oldValue, string newValue) => TextChanged(nameof(Location), oldValue, newValue, v => Location = v);
    partial void OnDescriptionChanged(string? oldValue, string newValue) => TextChanged(nameof(Description), oldValue, newValue, v => Description = v);
    partial void OnAchievementsChanged(string? oldValue, string newValue) => TextChanged(nameof(Achievements), oldValue, newValue, v => Achievements = v);
    partial void OnStartMonthNameChanged(string? value) => ValueChanged();
    partial void OnStartYearChanged(int? value) => ValueChanged();
    partial void OnEndMonthNameChanged(string? value) => ValueChanged();
    partial void OnEndYearChanged(int? value) => ValueChanged();
    partial void OnIsCurrentRoleChanged(bool value) => ValueChanged();

    public Experience ToModel(int order) => new()
    {
        Order = order,
        JobTitle = JobTitle,
        Company = Company,
        Location = Location,
        StartDate = DateOptions.ToDate(StartYear, StartMonthName),
        EndDate = IsCurrentRole ? null : DateOptions.ToDate(EndYear, EndMonthName),
        IsCurrentRole = IsCurrentRole,
        Description = Description,
        Achievements = AchievementLines.Parse(Achievements)
    };
}

public partial class EducationViewModel : ItemViewModelBase
{
    [ObservableProperty] private string _degree = "";
    [ObservableProperty] private string _fieldOfStudy = "";
    [ObservableProperty] private string _institution = "";
    [ObservableProperty] private string _location = "";
    [ObservableProperty] private int? _startYear;
    [ObservableProperty] private int? _endYear;
    [ObservableProperty] private bool _isCurrentlyStudying;
    [ObservableProperty] private string _grade = "";
    [ObservableProperty] private string _description = "";

    public EducationViewModel(Education edu, Action? onChanged = null, ITextEditRecorder? recorder = null)
        : base(onChanged, recorder)
    {
        Degree = edu.Degree;
        FieldOfStudy = edu.FieldOfStudy;
        Institution = edu.Institution;
        Location = edu.Location;
        StartYear = edu.StartDate?.Year;
        EndYear = edu.EndDate?.Year;
        IsCurrentlyStudying = edu.IsCurrentlyStudying;
        Grade = edu.Grade;
        Description = edu.Description;
    }

    partial void OnDegreeChanged(string? oldValue, string newValue) => TextChanged(nameof(Degree), oldValue, newValue, v => Degree = v);
    partial void OnFieldOfStudyChanged(string? oldValue, string newValue) => TextChanged(nameof(FieldOfStudy), oldValue, newValue, v => FieldOfStudy = v);
    partial void OnInstitutionChanged(string? oldValue, string newValue) => TextChanged(nameof(Institution), oldValue, newValue, v => Institution = v);
    partial void OnLocationChanged(string? oldValue, string newValue) => TextChanged(nameof(Location), oldValue, newValue, v => Location = v);
    partial void OnGradeChanged(string? oldValue, string newValue) => TextChanged(nameof(Grade), oldValue, newValue, v => Grade = v);
    partial void OnDescriptionChanged(string? oldValue, string newValue) => TextChanged(nameof(Description), oldValue, newValue, v => Description = v);
    partial void OnStartYearChanged(int? value) => ValueChanged();
    partial void OnEndYearChanged(int? value) => ValueChanged();
    partial void OnIsCurrentlyStudyingChanged(bool value) => ValueChanged();

    public Education ToModel(int order) => new()
    {
        Order = order,
        Degree = Degree,
        FieldOfStudy = FieldOfStudy,
        Institution = Institution,
        Location = Location,
        StartDate = DateOptions.ToDate(StartYear, null),
        EndDate = IsCurrentlyStudying ? null : DateOptions.ToDate(EndYear, null),
        IsCurrentlyStudying = IsCurrentlyStudying,
        Grade = Grade,
        Description = Description
    };
}

public partial class SkillViewModel : ItemViewModelBase
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _category = "";
    [ObservableProperty] private SkillLevel _level = SkillLevel.Intermediate;

    public static SkillLevel[] AvailableLevels { get; } =
    {
        SkillLevel.Beginner,
        SkillLevel.Elementary,
        SkillLevel.Intermediate,
        SkillLevel.Advanced,
        SkillLevel.Expert
    };

    public SkillViewModel(Skill skill, Action? onChanged = null, ITextEditRecorder? recorder = null)
        : base(onChanged, recorder)
    {
        Name = skill.Name;
        Category = skill.Category;
        Level = skill.Level;
    }

    partial void OnNameChanged(string? oldValue, string newValue) => TextChanged(nameof(Name), oldValue, newValue, v => Name = v);
    partial void OnCategoryChanged(string? oldValue, string newValue) => TextChanged(nameof(Category), oldValue, newValue, v => Category = v);
    partial void OnLevelChanged(SkillLevel value) => ValueChanged();

    public Skill ToModel(int order) => new()
    {
        Order = order,
        Name = Name,
        Category = Category,
        Level = Level
    };
}

public partial class LanguageViewModel : ItemViewModelBase
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private LanguageProficiency _proficiency = LanguageProficiency.Professional;

    public static LanguageProficiency[] AvailableProficiencies { get; } =
    {
        LanguageProficiency.Basic,
        LanguageProficiency.Conversational,
        LanguageProficiency.Professional,
        LanguageProficiency.Fluent,
        LanguageProficiency.Native
    };

    public LanguageViewModel(Language lang, Action? onChanged = null, ITextEditRecorder? recorder = null)
        : base(onChanged, recorder)
    {
        Name = lang.Name;
        Proficiency = lang.Proficiency;
    }

    partial void OnNameChanged(string? oldValue, string newValue) => TextChanged(nameof(Name), oldValue, newValue, v => Name = v);
    partial void OnProficiencyChanged(LanguageProficiency value) => ValueChanged();

    public Language ToModel(int order) => new()
    {
        Order = order,
        Name = Name,
        Proficiency = Proficiency
    };
}

public partial class CertificationViewModel : ItemViewModelBase
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _issuingOrganization = "";
    [ObservableProperty] private string? _issueMonthName;
    [ObservableProperty] private int? _issueYear;
    [ObservableProperty] private string? _expirationMonthName;
    [ObservableProperty] private int? _expirationYear;
    [ObservableProperty] private bool _doesNotExpire;
    [ObservableProperty] private string _credentialId = "";
    [ObservableProperty] private string _credentialUrl = "";

    public CertificationViewModel(Certification cert, Action? onChanged = null, ITextEditRecorder? recorder = null)
        : base(onChanged, recorder)
    {
        Name = cert.Name;
        IssuingOrganization = cert.IssuingOrganization;
        IssueMonthName = DateOptions.ToMonthName(cert.IssueDate);
        IssueYear = cert.IssueDate?.Year;
        ExpirationMonthName = DateOptions.ToMonthName(cert.ExpirationDate);
        ExpirationYear = cert.ExpirationDate?.Year;
        DoesNotExpire = cert.DoesNotExpire;
        CredentialId = cert.CredentialId;
        CredentialUrl = cert.CredentialUrl;
    }

    partial void OnNameChanged(string? oldValue, string newValue) => TextChanged(nameof(Name), oldValue, newValue, v => Name = v);
    partial void OnIssuingOrganizationChanged(string? oldValue, string newValue) => TextChanged(nameof(IssuingOrganization), oldValue, newValue, v => IssuingOrganization = v);
    partial void OnCredentialIdChanged(string? oldValue, string newValue) => TextChanged(nameof(CredentialId), oldValue, newValue, v => CredentialId = v);
    partial void OnCredentialUrlChanged(string? oldValue, string newValue) => TextChanged(nameof(CredentialUrl), oldValue, newValue, v => CredentialUrl = v);
    partial void OnIssueMonthNameChanged(string? value) => ValueChanged();
    partial void OnIssueYearChanged(int? value) => ValueChanged();
    partial void OnExpirationMonthNameChanged(string? value) => ValueChanged();
    partial void OnExpirationYearChanged(int? value) => ValueChanged();
    partial void OnDoesNotExpireChanged(bool value) => ValueChanged();

    public Certification ToModel(int order) => new()
    {
        Order = order,
        Name = Name,
        IssuingOrganization = IssuingOrganization,
        IssueDate = DateOptions.ToDate(IssueYear, IssueMonthName),
        ExpirationDate = DoesNotExpire ? null : DateOptions.ToDate(ExpirationYear, ExpirationMonthName),
        DoesNotExpire = DoesNotExpire,
        CredentialId = CredentialId,
        CredentialUrl = CredentialUrl
    };
}

public partial class ProjectViewModel : ItemViewModelBase
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _url = "";
    [ObservableProperty] private string _technologies = "";
    [ObservableProperty] private string _highlights = "";
    [ObservableProperty] private string? _startMonthName;
    [ObservableProperty] private int? _startYear;
    [ObservableProperty] private string? _endMonthName;
    [ObservableProperty] private int? _endYear;
    [ObservableProperty] private bool _isOngoing;

    public ProjectViewModel(Project proj, Action? onChanged = null, ITextEditRecorder? recorder = null)
        : base(onChanged, recorder)
    {
        Name = proj.Name;
        Description = proj.Description;
        Url = proj.Url;
        Technologies = string.Join(", ", proj.Technologies);
        Highlights = string.Join("\n", proj.Highlights);
        StartMonthName = DateOptions.ToMonthName(proj.StartDate);
        StartYear = proj.StartDate?.Year;
        EndMonthName = DateOptions.ToMonthName(proj.EndDate);
        EndYear = proj.EndDate?.Year;
        IsOngoing = proj.IsOngoing;
    }

    partial void OnNameChanged(string? oldValue, string newValue) => TextChanged(nameof(Name), oldValue, newValue, v => Name = v);
    partial void OnDescriptionChanged(string? oldValue, string newValue) => TextChanged(nameof(Description), oldValue, newValue, v => Description = v);
    partial void OnUrlChanged(string? oldValue, string newValue) => TextChanged(nameof(Url), oldValue, newValue, v => Url = v);
    partial void OnTechnologiesChanged(string? oldValue, string newValue) => TextChanged(nameof(Technologies), oldValue, newValue, v => Technologies = v);
    partial void OnHighlightsChanged(string? oldValue, string newValue) => TextChanged(nameof(Highlights), oldValue, newValue, v => Highlights = v);
    partial void OnStartMonthNameChanged(string? value) => ValueChanged();
    partial void OnStartYearChanged(int? value) => ValueChanged();
    partial void OnEndMonthNameChanged(string? value) => ValueChanged();
    partial void OnEndYearChanged(int? value) => ValueChanged();
    partial void OnIsOngoingChanged(bool value) => ValueChanged();

    public Project ToModel(int order) => new()
    {
        Order = order,
        Name = Name,
        Description = Description,
        Url = Url,
        StartDate = DateOptions.ToDate(StartYear, StartMonthName),
        EndDate = IsOngoing ? null : DateOptions.ToDate(EndYear, EndMonthName),
        IsOngoing = IsOngoing,
        Technologies = Technologies.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim()).ToList(),
        Highlights = Highlights.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(h => h.Trim()).Where(h => h.Length > 0).ToList()
    };
}

public partial class CustomSectionViewModel : ItemViewModelBase
{
    [ObservableProperty] private string _title = "";

    [ObservableProperty] private ObservableCollection<CustomSectionItemViewModel> _items = new();

    public CustomSectionViewModel(CustomSection section, Action? onChanged = null, ITextEditRecorder? recorder = null)
        : base(onChanged, recorder)
    {
        Title = section.Title;
        foreach (var item in section.Items.OrderBy(i => i.Order))
        {
            Items.Add(new CustomSectionItemViewModel(item, onChanged, recorder));
        }
    }

    partial void OnTitleChanged(string? oldValue, string newValue) => TextChanged(nameof(Title), oldValue, newValue, v => Title = v);

    [RelayCommand]
    private void AddItem()
    {
        Items.Add(new CustomSectionItemViewModel(new CustomSectionItem(), OnChanged, Recorder));
        ValueChanged();
    }

    [RelayCommand]
    private void RemoveItem(CustomSectionItemViewModel item)
    {
        Items.Remove(item);
        ValueChanged();
    }

    public CustomSection ToModel(int order) => new()
    {
        Order = order,
        Title = Title,
        Items = Items.Select((item, index) => item.ToModel(index)).ToList()
    };
}

public partial class CustomSectionItemViewModel : ItemViewModelBase
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string? _startMonthName;
    [ObservableProperty] private int? _startYear;
    [ObservableProperty] private string? _endMonthName;
    [ObservableProperty] private int? _endYear;

    public CustomSectionItemViewModel(CustomSectionItem item, Action? onChanged = null, ITextEditRecorder? recorder = null)
        : base(onChanged, recorder)
    {
        Title = item.Title;
        Subtitle = item.Subtitle;
        Description = item.Description;
        StartMonthName = DateOptions.ToMonthName(item.StartDate);
        StartYear = item.StartDate?.Year;
        EndMonthName = DateOptions.ToMonthName(item.EndDate);
        EndYear = item.EndDate?.Year;
    }

    partial void OnTitleChanged(string? oldValue, string newValue) => TextChanged(nameof(Title), oldValue, newValue, v => Title = v);
    partial void OnSubtitleChanged(string? oldValue, string newValue) => TextChanged(nameof(Subtitle), oldValue, newValue, v => Subtitle = v);
    partial void OnDescriptionChanged(string? oldValue, string newValue) => TextChanged(nameof(Description), oldValue, newValue, v => Description = v);
    partial void OnStartMonthNameChanged(string? value) => ValueChanged();
    partial void OnStartYearChanged(int? value) => ValueChanged();
    partial void OnEndMonthNameChanged(string? value) => ValueChanged();
    partial void OnEndYearChanged(int? value) => ValueChanged();

    public CustomSectionItem ToModel(int order) => new()
    {
        Order = order,
        Title = Title,
        Subtitle = Subtitle,
        Description = Description,
        StartDate = DateOptions.ToDate(StartYear, StartMonthName),
        EndDate = DateOptions.ToDate(EndYear, EndMonthName)
    };
}

public partial class SectionViewModel : ObservableObject
{
    [ObservableProperty] private SectionType _sectionType;
    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private int _order;

    private readonly Action? _onChanged;

    public SectionViewModel(SectionType type, bool isVisible, int order, Action? onChanged = null)
    {
        SectionType = type;
        DisplayName = SectionOrder.GetSectionDisplayName(type);
        IsVisible = isVisible;
        Order = order;
        _onChanged = onChanged;
    }

    partial void OnIsVisibleChanged(bool value) => _onChanged?.Invoke();
}

public partial class SpellSuggestionViewModel : ObservableObject
{
    [ObservableProperty] private string _misspelledWord = "";
    [ObservableProperty] private ObservableCollection<string> _suggestions = new();

    private readonly Action<string> _onApply;
    private readonly Action _onAddToDictionary;

    public SpellSuggestionViewModel(MisspelledWord word, Action<string> onApply, Action onAddToDictionary)
    {
        MisspelledWord = word.Word;
        Suggestions = new ObservableCollection<string>(word.Suggestions);
        _onApply = onApply;
        _onAddToDictionary = onAddToDictionary;
    }

    [RelayCommand]
    private void ApplySuggestion(string suggestion) => _onApply(suggestion);

    [RelayCommand]
    private void AddToDictionary() => _onAddToDictionary();
}

public class KeywordMatchViewModel
{
    public string Keyword { get; init; } = "";
    public string Category { get; init; } = "";
    public int JobCount { get; init; }
    public int ResumeCount { get; init; }
}

/// <summary>
/// One proposed rewrite awaiting review. Accepting it only ticks the underlying edit; nothing is
/// applied until <c>JobTailoringService.Apply</c> runs.
/// </summary>
public partial class TailoredEditViewModel : ObservableObject
{
    [ObservableProperty] private bool _accepted;

    public TailoredEditViewModel(TailoredEdit edit, string targetDescription)
    {
        Edit = edit;
        TargetDescription = targetDescription;
        Accepted = edit.Accepted;
    }

    public TailoredEdit Edit { get; }

    public string TargetDescription { get; }
    public string Original => Edit.Original;
    public string Proposed => Edit.Proposed;
    public string Rationale => Edit.Rationale;

    partial void OnAcceptedChanged(bool value) => Edit.Accepted = value;
}

/// <summary>One body paragraph of a cover letter.</summary>
public partial class LetterParagraphViewModel : ObservableObject
{
    [ObservableProperty] private string _text = "";

    private readonly Action? _onChanged;

    public LetterParagraphViewModel(string text, Action? onChanged = null)
    {
        Text = text;
        _onChanged = onChanged;
    }

    partial void OnTextChanged(string value) => _onChanged?.Invoke();
}

public class SyncConflictViewModel
{
    public string ResumeName { get; init; } = "";
    public DateTime LocalModified { get; init; }
    public DateTime RemoteModified { get; init; }

    public string Detail => $"Local {LocalModified:g} vs remote {RemoteModified:g}";
}

public class RemoteResumeViewModel
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public DateTime LastModified { get; init; }
    public long Size { get; init; }

    public string SizeDisplay => Size < 1024
        ? $"{Size} B"
        : Size < 1024 * 1024
            ? $"{Size / 1024.0:F1} KB"
            : $"{Size / (1024.0 * 1024.0):F1} MB";

    public string LastModifiedDisplay => LastModified.ToString("g");
}
