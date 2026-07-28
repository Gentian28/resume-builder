using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeBuilder.App.Services;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.SmartContent;
using ResumeBuilder.Core.SpellCheck;
using ResumeBuilder.Core.Sync;
using ResumeBuilder.Core.UndoRedo;
using ResumeBuilder.Core.Validation;
using ResumeBuilder.Data;
using ResumeBuilder.Templates;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace ResumeBuilder.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, ITextEditRecorder
{
    private const string DefaultResumeName = "Untitled Resume";

    private readonly AppServices _services;

    // Autosave and the manual Save both write the same resume; the gate keeps them from overlapping.
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    private DispatcherTimer? _autoSaveTimer;
    private DispatcherTimer? _previewDebounceTimer;
    private DispatcherTimer? _spellCheckTimer;

    private CancellationTokenSource? _previewCts;
    private string? _renderedSignature;

    private bool _isLoadingEditor;
    private bool _suppressUndoRecording;
    private DateTime? _lastSavedAt;

    [ObservableProperty]
    private Resume _currentResume = new();

    [ObservableProperty]
    private ObservableCollection<Resume> _savedResumes = new();

    [ObservableProperty]
    private ObservableCollection<TemplateInfo> _templates = new();

    [ObservableProperty]
    private TemplateInfo? _selectedTemplate;

    [ObservableProperty]
    private Bitmap? _previewImage;

    [ObservableProperty]
    private ObservableCollection<Bitmap> _previewPages = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// False unless this build was installed by Velopack *and* a newer release exists, so the
    /// prompt never appears when running from source or the portable build.
    /// </summary>
    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _updateStatusText = string.Empty;

    [ObservableProperty]
    private bool _showTemplateGallery;

    [ObservableProperty]
    private bool _showResumeList;

    [ObservableProperty]
    private double _previewZoom = 1.0;

    // Save state
    [ObservableProperty]
    private string _resumeName = DefaultResumeName;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _saveStateText = "No changes";

    // Personal Info properties for binding
    [ObservableProperty] private string _firstName = "";
    [ObservableProperty] private string _lastName = "";
    [ObservableProperty] private string _jobTitle = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _city = "";
    [ObservableProperty] private string _country = "";
    [ObservableProperty] private string _website = "";
    [ObservableProperty] private string _linkedIn = "";
    [ObservableProperty] private string _gitHub = "";
    [ObservableProperty] private string _summary = "";

    // Collections
    [ObservableProperty]
    private ObservableCollection<ExperienceViewModel> _experiences = new();

    [ObservableProperty]
    private ObservableCollection<EducationViewModel> _educationList = new();

    [ObservableProperty]
    private ObservableCollection<SkillViewModel> _skills = new();

    [ObservableProperty]
    private ObservableCollection<LanguageViewModel> _languages = new();

    [ObservableProperty]
    private ObservableCollection<CertificationViewModel> _certifications = new();

    [ObservableProperty]
    private ObservableCollection<ProjectViewModel> _projects = new();

    [ObservableProperty]
    private ObservableCollection<CustomSectionViewModel> _customSections = new();

    // Theme
    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private string _themeDisplayName = "System";

    // Photo
    [ObservableProperty]
    private Bitmap? _photoPreview;

    [ObservableProperty]
    private bool _hasPhoto;

    // Validation errors
    [ObservableProperty]
    private string? _emailError;

    [ObservableProperty]
    private string? _phoneError;

    [ObservableProperty]
    private string? _websiteError;

    [ObservableProperty]
    private string? _linkedInError;

    [ObservableProperty]
    private bool _hasValidationErrors;

    [ObservableProperty]
    private int _validationErrorCount;

    // Template Customization
    [ObservableProperty]
    private string _accentColor = TemplateSettings.DefaultAccentColor;

    [ObservableProperty]
    private string _secondaryColor = "#64748b";

    [ObservableProperty]
    private string _textColor = "#1f2937";

    [ObservableProperty]
    private string _headingColor = "#111827";

    [ObservableProperty]
    private string _selectedFontFamily = TemplateSettings.DefaultFontFamily;

    [ObservableProperty]
    private string _selectedHeadingFontFamily = TemplateSettings.DefaultFontFamily;

    [ObservableProperty]
    private float _fontSizeScale = 1.0f;

    [ObservableProperty]
    private float _lineSpacing = 1.4f;

    [ObservableProperty]
    private float _sectionSpacing = 15f;

    [ObservableProperty]
    private float _pageMargin = 30f;

    [ObservableProperty]
    private bool _showCustomizationPanel;

    [ObservableProperty]
    private bool _showAccentColorPicker;

    [ObservableProperty]
    private Avalonia.Media.Color _accentColorValue = Avalonia.Media.Color.Parse(TemplateSettings.DefaultAccentColor);

    private bool _isSyncingColor;

    // Section Ordering
    [ObservableProperty]
    private ObservableCollection<SectionViewModel> _sections = new();

    [ObservableProperty]
    private bool _showSectionOrderPanel;

    // Spell Check
    [ObservableProperty]
    private bool _isSpellCheckEnabled = true;

    [ObservableProperty]
    private int _summarySpellErrorCount;

    [ObservableProperty]
    private SpellCheckResult? _summarySpellResult;

    [ObservableProperty]
    private ObservableCollection<SpellSuggestionViewModel> _spellSuggestions = new();

    [ObservableProperty]
    private bool _showSpellSuggestions;

    // Undo/Redo
    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private string? _nextUndoDescription;

    [ObservableProperty]
    private string? _nextRedoDescription;

    // AI Features
    [ObservableProperty]
    private bool _showAiPanel;

    [ObservableProperty]
    private bool _isAiConfigured;

    [ObservableProperty]
    private string _aiApiKey = "";

    [ObservableProperty]
    private string _aiBaseUrl = LocalAiService.OpenAiBaseUrl;

    [ObservableProperty]
    private string _aiModel = LocalAiService.DefaultModel;

    [ObservableProperty]
    private bool _isAiLocal;

    [ObservableProperty]
    private string _aiPrivacyNotice = "";

    [ObservableProperty]
    private bool _isAiProcessing;

    [ObservableProperty]
    private string? _aiGeneratedSummary;

    [ObservableProperty]
    private ObservableCollection<string> _aiSkillSuggestions = new();

    [ObservableProperty]
    private ObservableCollection<string> _aiBulletSuggestions = new();

    [ObservableProperty]
    private string? _aiStatusMessage;

    // Tailor to job
    [ObservableProperty]
    private bool _showTailorPanel;

    [ObservableProperty]
    private string _jobDescriptionText = "";

    [ObservableProperty]
    private string _targetRoleText = "";

    [ObservableProperty]
    private int _keywordMatchScore;

    [ObservableProperty]
    private ObservableCollection<KeywordMatchViewModel> _matchedKeywords = new();

    [ObservableProperty]
    private ObservableCollection<string> _missingKeywords = new();

    [ObservableProperty]
    private ObservableCollection<string> _keywordSuggestions = new();

    [ObservableProperty]
    private ObservableCollection<string> _keywordWarnings = new();

    [ObservableProperty]
    private bool _hasKeywordAnalysis;

    [ObservableProperty]
    private bool _isTailoring;

    [ObservableProperty]
    private ObservableCollection<TailoredEditViewModel> _tailoredEdits = new();

    [ObservableProperty]
    private bool _hasTailoredEdits;

    [ObservableProperty]
    private ObservableCollection<string> _tailorSuggestedSkills = new();

    [ObservableProperty]
    private string? _tailorAiError;

    // Cloud Sync
    [ObservableProperty]
    private bool _showSyncPanel;

    [ObservableProperty]
    private bool _isSyncConfigured;

    [ObservableProperty]
    private string _syncFolderPath = "";

    [ObservableProperty]
    private string _syncStatusText = "Not configured";

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private int _syncUploadedCount;

    [ObservableProperty]
    private int _syncDownloadedCount;

    [ObservableProperty]
    private bool _hasSyncResult;

    [ObservableProperty]
    private ConflictResolution _selectedConflictResolution = ConflictResolution.NewestWins;

    [ObservableProperty]
    private ObservableCollection<SyncConflictViewModel> _syncConflicts = new();

    [ObservableProperty]
    private ObservableCollection<RemoteResumeViewModel> _remoteResumes = new();

    public MainWindowViewModel() : this(null!) { }

    public MainWindowViewModel(AppServices services)
    {
        _services = services;

        QuestPDF.Settings.License = LicenseType.Community;

        if (services != null)
        {
            LoadTemplates();
            _ = LoadSavedResumesAsync();
            SetupAutoSave();
            ResetToNewResume();
            InitializeTheme();
            InitializeUndoRedo();
            UpdateAiPrivacyNotice();
            _ = CheckForUpdatesAsync();
        }
    }

    /// <summary>
    /// Looks for a newer release and pre-downloads it, so "Restart &amp; update" is instant.
    /// Fire-and-forget on purpose: this must never delay startup, and
    /// <see cref="UpdateService"/> already swallows offline/feed errors rather than surfacing
    /// them — a failed update check is not the user's problem to solve.
    /// </summary>
    private async Task CheckForUpdatesAsync()
    {
        var updates = _services.UpdateService;
        if (!updates.IsSupported)
            return;

        if (!await updates.CheckAsync())
            return;

        // Download before offering the button, so accepting does not stall on a slow connection.
        if (!await updates.DownloadAsync())
            return;

        UpdateStatusText = $"Version {updates.AvailableVersion} ready";
        IsUpdateAvailable = true;
    }

    /// <summary>
    /// Applies the downloaded update and restarts. Saves first: ApplyUpdatesAndRestart exits the
    /// process, and losing an unedited résumé to an update would be inexcusable.
    /// </summary>
    [RelayCommand]
    private async Task ApplyUpdateAsync()
    {
        if (!IsUpdateAvailable)
            return;

        if (IsDirty)
            await SaveCurrentResumeAsync();

        _services.UpdateService.ApplyAndRestart();
    }

    public string[] AvailableFonts => TemplateSettings.AvailableFonts;
    public string[] PresetColors => TemplateSettings.PresetColors;
    public ConflictResolution[] ConflictResolutions { get; } = Enum.GetValues<ConflictResolution>();

    public static Avalonia.Data.Converters.FuncValueConverter<string, Avalonia.Media.IBrush> HexToColorConverter { get; } =
        new(hex =>
        {
            if (!TryParseHexColor(hex, out var color))
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Blue);

            return new Avalonia.Media.SolidColorBrush(color);
        });

    /// <summary>Indents a variant under the resume it was branched from.</summary>
    public static Avalonia.Data.Converters.FuncValueConverter<bool, Avalonia.Thickness> VariantIndentConverter { get; } =
        new(isVariant => isVariant
            ? new Avalonia.Thickness(30, 2, 0, 2)
            : new Avalonia.Thickness(0, 5, 0, 5));

    public static Avalonia.Data.Converters.FuncValueConverter<bool, Avalonia.Thickness> VariantBorderConverter { get; } =
        new(isVariant => isVariant
            ? new Avalonia.Thickness(3, 0, 0, 0)
            : new Avalonia.Thickness(0));

    // ---------------------------------------------------------------- Undo/Redo

    private void InitializeUndoRedo()
    {
        var manager = _services.UndoRedoManager;
        manager.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(UndoRedoManager.CanUndo))
                CanUndo = manager.CanUndo;
            else if (e.PropertyName == nameof(UndoRedoManager.CanRedo))
                CanRedo = manager.CanRedo;
            else if (e.PropertyName == nameof(UndoRedoManager.NextUndoDescription))
                NextUndoDescription = manager.NextUndoDescription;
            else if (e.PropertyName == nameof(UndoRedoManager.NextRedoDescription))
                NextRedoDescription = manager.NextRedoDescription;
        };

        manager.ActionUndone += action =>
        {
            MarkDirty();
            UpdatePreviewDebounced();
            StatusMessage = $"Undo: {action.Description}";
        };

        manager.ActionRedone += action =>
        {
            MarkDirty();
            UpdatePreviewDebounced();
            StatusMessage = $"Redo: {action.Description}";
        };
    }

    [RelayCommand]
    private void Undo()
    {
        if (_services?.UndoRedoManager == null) return;

        _suppressUndoRecording = true;
        try
        {
            _services.UndoRedoManager.Undo();
        }
        finally
        {
            _suppressUndoRecording = false;
        }
    }

    [RelayCommand]
    private void Redo()
    {
        if (_services?.UndoRedoManager == null) return;

        _suppressUndoRecording = true;
        try
        {
            _services.UndoRedoManager.Redo();
        }
        finally
        {
            _suppressUndoRecording = false;
        }
    }

    /// <summary>
    /// The undo entry is recorded after the property already changed, so the setter here is only ever
    /// run by undo/redo - where the manager suppresses re-recording.
    /// </summary>
    public void RecordTextEdit(string fieldKey, string oldValue, string newValue, Action<string> setter, string description)
    {
        if (!CanRecordUndo() || string.Equals(oldValue, newValue, StringComparison.Ordinal))
            return;

        _services.UndoRedoManager.RecordAction(
            new TextEditAction(fieldKey, oldValue, newValue, setter, description));
    }

    private void RecordEdit(string field, string oldValue, string newValue, Action<string> setter) =>
        RecordTextEdit($"Resume.{field}", oldValue, newValue, setter, $"Edit {field}");

    /// <summary>
    /// Numeric customization is round-tripped through invariant strings so that a slider drag merges
    /// into one undo entry instead of flooding the history with one entry per tick.
    /// </summary>
    private void RecordNumericEdit(string field, float oldValue, float newValue, Action<float> setter) =>
        RecordTextEdit(
            $"Style.{field}",
            oldValue.ToString(CultureInfo.InvariantCulture),
            newValue.ToString(CultureInfo.InvariantCulture),
            v => setter(float.Parse(v, CultureInfo.InvariantCulture)),
            $"Change {field}");

    private bool CanRecordUndo() =>
        !_isLoadingEditor &&
        !_suppressUndoRecording &&
        _services?.UndoRedoManager != null &&
        !_services.UndoRedoManager.IsExecutingAction;

    // ---------------------------------------------------------------- Theme

    private void InitializeTheme()
    {
        UpdateThemeDisplay();
        _services.ThemeService.ThemeChanged += _ => UpdateThemeDisplay();
    }

    private void UpdateThemeDisplay()
    {
        IsDarkTheme = _services.ThemeService.IsDarkTheme;
        ThemeDisplayName = _services.ThemeService.CurrentTheme switch
        {
            var t when t == Avalonia.Styling.ThemeVariant.Light => "Light",
            var t when t == Avalonia.Styling.ThemeVariant.Dark => "Dark",
            _ => "System"
        };
    }

    [RelayCommand]
    private void SetLightTheme()
    {
        _services.ThemeService.SetLightTheme();
        StatusMessage = "Theme changed to Light";
    }

    [RelayCommand]
    private void SetDarkTheme()
    {
        _services.ThemeService.SetDarkTheme();
        StatusMessage = "Theme changed to Dark";
    }

    [RelayCommand]
    private void SetSystemTheme()
    {
        _services.ThemeService.SetSystemTheme();
        StatusMessage = "Theme changed to System";
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _services.ThemeService.ToggleTheme();
        StatusMessage = $"Theme changed to {ThemeDisplayName}";
    }

    // ---------------------------------------------------------------- Validation

    private readonly EmailRule _emailValidator = new();
    private readonly PhoneRule _phoneValidator = new();
    private readonly UrlRule _urlValidator = new();
    private readonly LinkedInRule _linkedInValidator = new();

    private void ValidateEmail(string? value)
    {
        EmailError = string.IsNullOrWhiteSpace(value) ? null : Error(_emailValidator.Validate(value));
        UpdateValidationState();
    }

    private void ValidatePhone(string? value)
    {
        PhoneError = string.IsNullOrWhiteSpace(value) ? null : Error(_phoneValidator.Validate(value));
        UpdateValidationState();
    }

    private void ValidateWebsite(string? value)
    {
        WebsiteError = string.IsNullOrWhiteSpace(value) ? null : Error(_urlValidator.Validate(value));
        UpdateValidationState();
    }

    private void ValidateLinkedIn(string? value)
    {
        LinkedInError = string.IsNullOrWhiteSpace(value) ? null : Error(_linkedInValidator.Validate(value));
        UpdateValidationState();
    }

    private static string? Error(ValidationResult result) => result.IsValid ? null : result.ErrorMessage;

    private void UpdateValidationState()
    {
        var errorCount = 0;
        if (EmailError != null) errorCount++;
        if (PhoneError != null) errorCount++;
        if (WebsiteError != null) errorCount++;
        if (LinkedInError != null) errorCount++;

        ValidationErrorCount = errorCount;
        HasValidationErrors = errorCount > 0;
    }

    // ---------------------------------------------------------------- Photo

    private readonly ImageService _imageService = new();

    [RelayCommand]
    private async Task UploadPhotoAsync()
    {
        try
        {
            var topLevel = MainWindow;
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Select Photo",
                AllowMultiple = false,
                FileTypeFilter = new[] { ImageService.ImageFileTypes }
            });

            if (files.Count == 0) return;

            var imageBytes = await _imageService.LoadAndResizeImageAsync(files[0]);
            if (imageBytes != null)
            {
                CurrentResume.PersonalInfo.Photo = imageBytes;
                PhotoPreview = _imageService.BytesToBitmap(imageBytes);
                HasPhoto = true;
                MarkDirty();
                UpdatePreviewDebounced();
                StatusMessage = "Photo uploaded";
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Photo upload failed", ex.Message);
        }
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        CurrentResume.PersonalInfo.Photo = null;
        PhotoPreview = null;
        HasPhoto = false;
        MarkDirty();
        UpdatePreviewDebounced();
        StatusMessage = "Photo removed";
    }

    private void LoadPhotoPreview()
    {
        if (CurrentResume.PersonalInfo.Photo is { Length: > 0 } photo)
        {
            PhotoPreview = _imageService.BytesToBitmap(photo);
            HasPhoto = true;
        }
        else
        {
            PhotoPreview = null;
            HasPhoto = false;
        }
    }

    // ---------------------------------------------------------------- Editor <-> model

    private void LoadTemplates()
    {
        Templates.Clear();
        foreach (var template in _services.TemplateRegistry.GetAllTemplateInfos())
        {
            Templates.Add(template);
        }
        SelectedTemplate = Templates.FirstOrDefault(t => t.Id == "modern") ?? Templates.FirstOrDefault();
    }

    /// <summary>
    /// The gallery's own list, carrying a rendered preview per template.
    ///
    /// Separate from <see cref="Templates"/> so the plain <see cref="TemplateInfo"/> list stays
    /// the binding source for the template dropdown and the status bar. Only the gallery needs
    /// thumbnails, and only the gallery should pay for rendering them.
    /// </summary>
    public ObservableCollection<TemplateCardViewModel> TemplateCards { get; } = new();

    /// <summary>
    /// Renders previews the first time the gallery is opened, never at start-up. 25 QuestPDF
    /// renders is real work, and most sessions never open the gallery at all.
    /// </summary>
    partial void OnShowTemplateGalleryChanged(bool value)
    {
        if (value)
            _ = LoadTemplateCardsAsync();
    }

    private async Task LoadTemplateCardsAsync()
    {
        if (TemplateCards.Count == 0)
        {
            foreach (var info in Templates)
            {
                TemplateCards.Add(new TemplateCardViewModel(info, _services.TemplateThumbnailService));
            }
        }

        // Sequentially rather than all at once: each render is CPU-bound, and 25 in parallel would
        // saturate the machine and make the first card appear later, not sooner. This way cards
        // fill in from the top while the user is still reading the first row.
        foreach (var card in TemplateCards)
        {
            await card.LoadThumbnailAsync().ConfigureAwait(true);
        }
    }

    private async Task LoadSavedResumesAsync()
    {
        try
        {
            var resumes = await _services.Repository.GetAllAsync();
            SavedResumes.Clear();
            foreach (var resume in OrderWithVariants(resumes))
            {
                SavedResumes.Add(resume);
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Could not load your resumes", ex.Message);
        }
    }

    /// <summary>
    /// Lists each base resume followed by the variants branched from it. A variant whose base has
    /// been deleted keeps its own place in the list rather than disappearing.
    /// </summary>
    private static IEnumerable<Resume> OrderWithVariants(List<Resume> resumes)
    {
        var variantsByBase = resumes.Where(r => r.IsVariant).ToLookup(r => r.BaseResumeId!.Value);
        var ids = resumes.Select(r => r.Id).ToHashSet();

        foreach (var resume in resumes.Where(r => !r.IsVariant || !ids.Contains(r.BaseResumeId!.Value)))
        {
            yield return resume;

            foreach (var variant in variantsByBase[resume.Id])
            {
                yield return variant;
            }
        }
    }

    private void ResetToNewResume()
    {
        var resume = new Resume
        {
            Name = DefaultResumeName,
            SelectedTemplateId = SelectedTemplate?.Id ?? "modern"
        };

        _services.UndoRedoManager.Clear();
        CurrentResume = resume;
        LoadResumeIntoEditor(resume);
        IsDirty = false;
        _lastSavedAt = null;
        UpdateSaveState();
        UpdatePreviewDebounced();
        StatusMessage = "New resume created";
    }

    private void LoadResumeIntoEditor(Resume resume)
    {
        _isLoadingEditor = true;
        try
        {
            resume.SectionOrder.EnsureAllSectionsPresent();

            ResumeName = resume.Name;
            JobDescriptionText = resume.JobDescription;
            TargetRoleText = resume.TargetRole;
            FirstName = resume.PersonalInfo.FirstName;
            LastName = resume.PersonalInfo.LastName;
            JobTitle = resume.PersonalInfo.JobTitle;
            Email = resume.PersonalInfo.Email;
            Phone = resume.PersonalInfo.Phone;
            City = resume.PersonalInfo.City;
            Country = resume.PersonalInfo.Country;
            Website = resume.PersonalInfo.Website;
            LinkedIn = resume.PersonalInfo.LinkedIn;
            GitHub = resume.PersonalInfo.GitHub;
            Summary = resume.Summary;

            Experiences = new ObservableCollection<ExperienceViewModel>(
                resume.Experiences.OrderBy(e => e.Order).Select(e => new ExperienceViewModel(e, OnSubViewModelChanged, this)));

            EducationList = new ObservableCollection<EducationViewModel>(
                resume.EducationList.OrderBy(e => e.Order).Select(e => new EducationViewModel(e, OnSubViewModelChanged, this)));

            Skills = new ObservableCollection<SkillViewModel>(
                resume.Skills.OrderBy(s => s.Order).Select(s => new SkillViewModel(s, OnSubViewModelChanged, this)));

            Languages = new ObservableCollection<LanguageViewModel>(
                resume.Languages.OrderBy(l => l.Order).Select(l => new LanguageViewModel(l, OnSubViewModelChanged, this)));

            Certifications = new ObservableCollection<CertificationViewModel>(
                resume.Certifications.OrderBy(c => c.Order).Select(c => new CertificationViewModel(c, OnSubViewModelChanged, this)));

            Projects = new ObservableCollection<ProjectViewModel>(
                resume.Projects.OrderBy(p => p.Order).Select(p => new ProjectViewModel(p, OnSubViewModelChanged, this)));

            CustomSections = new ObservableCollection<CustomSectionViewModel>(
                resume.CustomSections.OrderBy(c => c.Order).Select(c => new CustomSectionViewModel(c, OnSubViewModelChanged, this)));

            SelectedTemplate = Templates.FirstOrDefault(t => t.Id == resume.SelectedTemplateId) ?? Templates.FirstOrDefault();

            LoadPhotoPreview();
            LoadCustomizationFromResume();
            LoadSectionsFromResume();

            ValidateEmail(Email);
            ValidatePhone(Phone);
            ValidateWebsite(Website);
            ValidateLinkedIn(LinkedIn);
        }
        finally
        {
            _isLoadingEditor = false;
        }
    }

    private void SyncEditorToResume()
    {
        if (_isLoadingEditor) return;

        CurrentResume.Name = string.IsNullOrWhiteSpace(ResumeName) ? DefaultResumeName : ResumeName.Trim();
        CurrentResume.JobDescription = JobDescriptionText;
        CurrentResume.TargetRole = TargetRoleText;
        CurrentResume.PersonalInfo.FirstName = FirstName;
        CurrentResume.PersonalInfo.LastName = LastName;
        CurrentResume.PersonalInfo.JobTitle = JobTitle;
        CurrentResume.PersonalInfo.Email = Email;
        CurrentResume.PersonalInfo.Phone = Phone;
        CurrentResume.PersonalInfo.City = City;
        CurrentResume.PersonalInfo.Country = Country;
        CurrentResume.PersonalInfo.Website = Website;
        CurrentResume.PersonalInfo.LinkedIn = LinkedIn;
        CurrentResume.PersonalInfo.GitHub = GitHub;
        CurrentResume.Summary = Summary;
        CurrentResume.SelectedTemplateId = SelectedTemplate?.Id ?? "modern";

        CurrentResume.Experiences = Experiences.Select((e, i) => e.ToModel(i)).ToList();
        CurrentResume.EducationList = EducationList.Select((e, i) => e.ToModel(i)).ToList();
        CurrentResume.Skills = Skills.Select((s, i) => s.ToModel(i)).ToList();
        CurrentResume.Languages = Languages.Select((l, i) => l.ToModel(i)).ToList();
        CurrentResume.Certifications = Certifications.Select((c, i) => c.ToModel(i)).ToList();
        CurrentResume.Projects = Projects.Select((p, i) => p.ToModel(i)).ToList();
        CurrentResume.CustomSections = CustomSections.Select((c, i) => c.ToModel(i)).ToList();
    }

    private void OnSubViewModelChanged() => OnEditorChanged();

    private void OnEditorChanged()
    {
        if (_isLoadingEditor) return;

        SyncEditorToResume();
        MarkDirty();
        UpdatePreviewDebounced();
    }

    partial void OnResumeNameChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(ResumeName), oldValue ?? "", newValue, v => ResumeName = v);
        OnEditorChanged();
    }

    partial void OnFirstNameChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(FirstName), oldValue ?? "", newValue, v => FirstName = v);
        OnEditorChanged();
    }

    partial void OnLastNameChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(LastName), oldValue ?? "", newValue, v => LastName = v);
        OnEditorChanged();
    }

    partial void OnJobTitleChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(JobTitle), oldValue ?? "", newValue, v => JobTitle = v);
        OnEditorChanged();
    }

    partial void OnCityChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(City), oldValue ?? "", newValue, v => City = v);
        OnEditorChanged();
    }

    partial void OnCountryChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(Country), oldValue ?? "", newValue, v => Country = v);
        OnEditorChanged();
    }

    partial void OnGitHubChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(GitHub), oldValue ?? "", newValue, v => GitHub = v);
        OnEditorChanged();
    }

    partial void OnEmailChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(Email), oldValue ?? "", newValue, v => Email = v);
        ValidateEmail(newValue);
        OnEditorChanged();
    }

    partial void OnPhoneChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(Phone), oldValue ?? "", newValue, v => Phone = v);
        ValidatePhone(newValue);
        OnEditorChanged();
    }

    partial void OnWebsiteChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(Website), oldValue ?? "", newValue, v => Website = v);
        ValidateWebsite(newValue);
        OnEditorChanged();
    }

    partial void OnLinkedInChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(LinkedIn), oldValue ?? "", newValue, v => LinkedIn = v);
        ValidateLinkedIn(newValue);
        OnEditorChanged();
    }

    partial void OnSummaryChanged(string? oldValue, string newValue)
    {
        RecordEdit(nameof(Summary), oldValue ?? "", newValue, v => Summary = v);
        OnEditorChanged();
        CheckSpellingDebounced();
    }

    partial void OnSelectedTemplateChanged(TemplateInfo? value)
    {
        if (value == null || _isLoadingEditor) return;

        CurrentResume.SelectedTemplateId = value.Id;
        CurrentResume.TemplateSettings.ApplyTemplateDefaults(value);

        _isLoadingEditor = true;
        try
        {
            LoadCustomizationFromResume();
        }
        finally
        {
            _isLoadingEditor = false;
        }

        MarkDirty();
        UpdatePreviewDebounced();
    }

    // ---------------------------------------------------------------- Dirty state

    private void MarkDirty()
    {
        if (_isLoadingEditor) return;

        IsDirty = true;
        UpdateSaveState();
    }

    private void UpdateSaveState()
    {
        SaveStateText = IsSaving
            ? "Saving..."
            : IsDirty
                ? "Unsaved changes"
                : _lastSavedAt.HasValue
                    ? $"Saved at {_lastSavedAt:HH:mm:ss}"
                    : "No changes";
    }

    /// <summary>A resume with nothing in it is never worth autosaving as a new record.</summary>
    private bool HasMeaningfulContent() =>
        !string.IsNullOrWhiteSpace(FirstName) ||
        !string.IsNullOrWhiteSpace(LastName) ||
        !string.IsNullOrWhiteSpace(JobTitle) ||
        !string.IsNullOrWhiteSpace(Email) ||
        !string.IsNullOrWhiteSpace(Phone) ||
        !string.IsNullOrWhiteSpace(Summary) ||
        Experiences.Count > 0 ||
        EducationList.Count > 0 ||
        Skills.Count > 0 ||
        Languages.Count > 0 ||
        Certifications.Count > 0 ||
        Projects.Count > 0 ||
        CustomSections.Count > 0;

    // ---------------------------------------------------------------- Customization

    partial void OnAccentColorChanged(string? oldValue, string newValue)
    {
        MarkAccentCustomized();
        RecordTextEdit("Style.AccentColor", oldValue ?? "", newValue, v => AccentColor = v, "Change accent color");
        OnCustomizationChanged();
        SyncAccentColorToColorPicker();
    }

    partial void OnSecondaryColorChanged(string? oldValue, string newValue)
    {
        RecordTextEdit("Style.SecondaryColor", oldValue ?? "", newValue, v => SecondaryColor = v, "Change secondary color");
        OnCustomizationChanged();
    }

    partial void OnTextColorChanged(string? oldValue, string newValue)
    {
        RecordTextEdit("Style.TextColor", oldValue ?? "", newValue, v => TextColor = v, "Change text color");
        OnCustomizationChanged();
    }

    partial void OnHeadingColorChanged(string? oldValue, string newValue)
    {
        RecordTextEdit("Style.HeadingColor", oldValue ?? "", newValue, v => HeadingColor = v, "Change heading color");
        OnCustomizationChanged();
    }

    partial void OnSelectedFontFamilyChanged(string? oldValue, string newValue)
    {
        MarkFontCustomized();
        RecordTextEdit("Style.FontFamily", oldValue ?? "", newValue, v => SelectedFontFamily = v, "Change body font");
        OnCustomizationChanged();
    }

    partial void OnSelectedHeadingFontFamilyChanged(string? oldValue, string newValue)
    {
        MarkFontCustomized();
        RecordTextEdit("Style.HeadingFontFamily", oldValue ?? "", newValue, v => SelectedHeadingFontFamily = v, "Change heading font");
        OnCustomizationChanged();
    }

    partial void OnFontSizeScaleChanged(float oldValue, float newValue)
    {
        RecordNumericEdit(nameof(FontSizeScale), oldValue, newValue, v => FontSizeScale = v);
        OnCustomizationChanged();
    }

    partial void OnLineSpacingChanged(float oldValue, float newValue)
    {
        RecordNumericEdit(nameof(LineSpacing), oldValue, newValue, v => LineSpacing = v);
        OnCustomizationChanged();
    }

    partial void OnSectionSpacingChanged(float oldValue, float newValue)
    {
        RecordNumericEdit(nameof(SectionSpacing), oldValue, newValue, v => SectionSpacing = v);
        OnCustomizationChanged();
    }

    partial void OnPageMarginChanged(float oldValue, float newValue)
    {
        RecordNumericEdit(nameof(PageMargin), oldValue, newValue, v => PageMargin = v);
        OnCustomizationChanged();
    }

    private void OnCustomizationChanged()
    {
        if (_isLoadingEditor) return;

        SyncCustomizationToResume();
        MarkDirty();
        UpdatePreviewDebounced();
    }

    private void MarkAccentCustomized()
    {
        if (!_isLoadingEditor)
            CurrentResume.TemplateSettings.IsAccentColorCustomized = true;
    }

    private void MarkFontCustomized()
    {
        if (!_isLoadingEditor)
            CurrentResume.TemplateSettings.IsFontCustomized = true;
    }

    private void SyncCustomizationToResume()
    {
        var settings = CurrentResume.TemplateSettings;
        settings.AccentColor = AccentColor;
        settings.SecondaryColor = SecondaryColor;
        settings.TextColor = TextColor;
        settings.HeadingColor = HeadingColor;
        settings.FontFamily = SelectedFontFamily;
        settings.HeadingFontFamily = SelectedHeadingFontFamily;
        settings.FontSizeScale = FontSizeScale;
        settings.LineSpacing = LineSpacing;
        settings.SectionSpacing = SectionSpacing;
        settings.PageMargin = PageMargin;

        CurrentResume.SyncLegacyStyling();
    }

    private void LoadCustomizationFromResume()
    {
        var settings = CurrentResume.TemplateSettings;
        AccentColor = settings.AccentColor;
        SecondaryColor = settings.SecondaryColor;
        TextColor = settings.TextColor;
        HeadingColor = settings.HeadingColor;
        SelectedFontFamily = settings.FontFamily;
        SelectedHeadingFontFamily = settings.HeadingFontFamily;
        FontSizeScale = settings.FontSizeScale;
        LineSpacing = settings.LineSpacing;
        SectionSpacing = settings.SectionSpacing;
        PageMargin = settings.PageMargin;
        SyncAccentColorToColorPicker();
    }

    [RelayCommand]
    private void SetAccentColor(string color) => AccentColor = color;

    [RelayCommand]
    private void ToggleAccentColorPicker() => ShowAccentColorPicker = !ShowAccentColorPicker;

    [RelayCommand]
    private void ResetCustomization()
    {
        var defaults = new TemplateSettings();
        AccentColor = defaults.AccentColor;
        SecondaryColor = defaults.SecondaryColor;
        TextColor = defaults.TextColor;
        HeadingColor = defaults.HeadingColor;
        SelectedFontFamily = defaults.FontFamily;
        SelectedHeadingFontFamily = defaults.HeadingFontFamily;
        FontSizeScale = defaults.FontSizeScale;
        LineSpacing = defaults.LineSpacing;
        SectionSpacing = defaults.SectionSpacing;
        PageMargin = defaults.PageMargin;

        CurrentResume.TemplateSettings.IsAccentColorCustomized = false;
        CurrentResume.TemplateSettings.IsFontCustomized = false;

        if (SelectedTemplate != null)
        {
            CurrentResume.TemplateSettings.ApplyTemplateDefaults(SelectedTemplate);

            _isLoadingEditor = true;
            try
            {
                LoadCustomizationFromResume();
            }
            finally
            {
                _isLoadingEditor = false;
            }
        }

        MarkDirty();
        UpdatePreviewDebounced();
        StatusMessage = "Customization reset to template defaults";
    }

    partial void OnAccentColorValueChanged(Avalonia.Media.Color value)
    {
        if (_isSyncingColor) return;

        _isSyncingColor = true;
        try
        {
            var newHex = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
            if (!string.Equals(AccentColor, newHex, StringComparison.OrdinalIgnoreCase))
            {
                AccentColor = newHex;
            }
        }
        finally
        {
            _isSyncingColor = false;
        }
    }

    private void SyncAccentColorToColorPicker()
    {
        if (_isSyncingColor) return;

        _isSyncingColor = true;
        try
        {
            if (TryParseHexColor(AccentColor, out var color) && color != AccentColorValue)
            {
                AccentColorValue = color;
            }
        }
        finally
        {
            _isSyncingColor = false;
        }
    }

    private static bool TryParseHexColor(string? hex, out Avalonia.Media.Color color)
    {
        color = default;
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length != 7)
            return false;

        return Avalonia.Media.Color.TryParse(hex, out color);
    }

    // ---------------------------------------------------------------- Section ordering

    private void LoadSectionsFromResume()
    {
        Sections.Clear();
        var sectionOrder = CurrentResume.SectionOrder;

        for (var i = 0; i < sectionOrder.OrderedSections.Count; i++)
        {
            var type = sectionOrder.OrderedSections[i];
            Sections.Add(new SectionViewModel(type, sectionOrder.IsSectionVisible(type), i, OnSectionChanged));
        }
    }

    private void SyncSectionsToResume()
    {
        CurrentResume.SectionOrder.OrderedSections = Sections.Select(s => s.SectionType).ToList();
        foreach (var section in Sections)
        {
            CurrentResume.SectionOrder.SetSectionVisibility(section.SectionType, section.IsVisible);
        }
    }

    private void OnSectionChanged()
    {
        if (_isLoadingEditor) return;

        SyncSectionsToResume();
        MarkDirty();
        UpdatePreviewDebounced();
    }

    [RelayCommand]
    private void MoveSectionUp(SectionViewModel section)
    {
        var index = Sections.IndexOf(section);
        if (index > 0)
        {
            Sections.Move(index, index - 1);
            UpdateSectionOrders();
            OnSectionChanged();
        }
    }

    [RelayCommand]
    private void MoveSectionDown(SectionViewModel section)
    {
        var index = Sections.IndexOf(section);
        if (index >= 0 && index < Sections.Count - 1)
        {
            Sections.Move(index, index + 1);
            UpdateSectionOrders();
            OnSectionChanged();
        }
    }

    [RelayCommand]
    private void ResetSectionOrder()
    {
        CurrentResume.SectionOrder = new SectionOrder();
        LoadSectionsFromResume();
        MarkDirty();
        UpdatePreviewDebounced();
        StatusMessage = "Section order reset to default";
    }

    private void UpdateSectionOrders()
    {
        for (var i = 0; i < Sections.Count; i++)
        {
            Sections[i].Order = i;
        }
    }

    // ---------------------------------------------------------------- Spell check

    private void CheckSpellingDebounced()
    {
        if (!IsSpellCheckEnabled || _services?.SpellChecker == null)
            return;

        if (_spellCheckTimer == null)
        {
            _spellCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _spellCheckTimer.Tick += (_, _) =>
            {
                _spellCheckTimer!.Stop();
                CheckSummarySpelling();
            };
        }

        _spellCheckTimer.Stop();
        _spellCheckTimer.Start();
    }

    private void CheckSummarySpelling()
    {
        if (!IsSpellCheckEnabled || _services?.SpellChecker is not { IsInitialized: true } checker)
        {
            SummarySpellErrorCount = 0;
            SummarySpellResult = null;
            return;
        }

        var result = checker.CheckText(Summary);
        SummarySpellResult = result;
        SummarySpellErrorCount = result.ErrorCount;
    }

    [RelayCommand]
    private void ShowSpellingSuggestions()
    {
        if (SummarySpellResult is not { HasErrors: true } result)
            return;

        SpellSuggestions.Clear();
        foreach (var misspelled in result.MisspelledWords.Take(10))
        {
            var word = misspelled;
            SpellSuggestions.Add(new SpellSuggestionViewModel(
                word,
                replacement => ApplySpellingSuggestion(word, replacement),
                () => AddToPersonalDictionary(word.Word)));
        }
        ShowSpellSuggestions = true;
    }

    /// <summary>
    /// Replaces the one occurrence the spell checker flagged. A text-wide Replace would also rewrite
    /// every other match, including the word as a substring of a correctly spelled one.
    /// </summary>
    private void ApplySpellingSuggestion(MisspelledWord misspelled, string replacement)
    {
        var text = Summary;
        var start = misspelled.StartIndex;
        var length = misspelled.Length;

        if (start < 0 || length <= 0 || start + length > text.Length ||
            !string.Equals(text.Substring(start, length), misspelled.Word, StringComparison.Ordinal))
        {
            StatusMessage = "The text changed - re-run the spell check";
            ShowSpellSuggestions = false;
            CheckSummarySpelling();
            return;
        }

        Summary = text.Remove(start, length).Insert(start, replacement);
        ShowSpellSuggestions = false;
        CheckSummarySpelling();
    }

    private void AddToPersonalDictionary(string word)
    {
        _services?.SpellChecker?.AddToPersonalDictionary(word);
        ShowSpellSuggestions = false;
        CheckSummarySpelling();
        StatusMessage = $"Added '{word}' to personal dictionary";
    }

    [RelayCommand]
    private void CloseSpellSuggestions() => ShowSpellSuggestions = false;

    [RelayCommand]
    private void ToggleSpellCheck()
    {
        IsSpellCheckEnabled = !IsSpellCheckEnabled;
        if (IsSpellCheckEnabled)
        {
            CheckSummarySpelling();
            StatusMessage = "Spell check enabled";
        }
        else
        {
            SummarySpellErrorCount = 0;
            SummarySpellResult = null;
            StatusMessage = "Spell check disabled";
        }
    }

    // ---------------------------------------------------------------- AI

    [RelayCommand]
    private void ApplyAiSettings()
    {
        if (_services?.AiService is not LocalAiService ai)
            return;

        var baseUrl = string.IsNullOrWhiteSpace(AiBaseUrl) ? LocalAiService.OpenAiBaseUrl : AiBaseUrl.Trim();
        var model = string.IsNullOrWhiteSpace(AiModel) ? LocalAiService.DefaultModel : AiModel.Trim();

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            AiStatusMessage = "That base URL is not a valid absolute URL";
            return;
        }

        if (!uri.IsLoopback && string.IsNullOrWhiteSpace(AiApiKey))
        {
            AiStatusMessage = "A remote endpoint needs an API key";
            return;
        }

        // ConfigureLocal is the only way to move the endpoint; Configure then attaches the key.
        ai.ConfigureLocal(baseUrl, model);

        if (!string.IsNullOrWhiteSpace(AiApiKey))
        {
            ai.Configure(AiApiKey.Trim(), model);
        }

        IsAiConfigured = ai.IsConfigured;
        UpdateAiPrivacyNotice();
        AiStatusMessage = $"Ready - using {ai.Model} at {ai.BaseUrl}";
    }

    [RelayCommand]
    private void UseLocalAiEndpoint()
    {
        AiBaseUrl = "http://localhost:11434/v1";
        AiModel = "llama3";
        AiApiKey = "";
        ApplyAiSettings();
    }

    [RelayCommand]
    private void UseOpenAiEndpoint()
    {
        AiBaseUrl = LocalAiService.OpenAiBaseUrl;
        AiModel = LocalAiService.DefaultModel;
    }

    private void UpdateAiPrivacyNotice()
    {
        if (_services?.AiService is not LocalAiService ai)
            return;

        IsAiLocal = ai.IsLocal;
        AiPrivacyNotice = ai.IsLocal
            ? $"Requests go to {ai.BaseUrl} on this machine. Nothing leaves your computer, and no API key is needed."
            : $"Your resume text is sent to {ai.BaseUrl} (model {ai.Model}). The API key is held in memory for this session only and is never written to disk.";
    }

    [RelayCommand]
    private async Task GenerateAiSummaryAsync()
    {
        if (_services?.AiService is not { IsConfigured: true } ai)
        {
            AiStatusMessage = "Configure the AI endpoint first";
            return;
        }

        IsAiProcessing = true;
        AiStatusMessage = "Generating summary...";

        try
        {
            var experiences = Experiences
                .Where(e => !string.IsNullOrWhiteSpace(e.JobTitle))
                .Select(e => $"{e.JobTitle} at {e.Company}: {e.Description}")
                .ToList();

            var skills = Skills
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => s.Name)
                .ToList();

            var result = await ai.GenerateSummaryAsync(JobTitle, experiences, skills);

            if (result.Success && result.Data != null)
            {
                AiGeneratedSummary = result.Data;
                AiStatusMessage = "Summary generated - click Apply to use it.";
            }
            else
            {
                AiStatusMessage = result.ErrorMessage ?? "Failed to generate summary";
                await DialogService.ShowErrorAsync("AI request failed", AiStatusMessage);
            }
        }
        catch (Exception ex)
        {
            AiStatusMessage = ex.Message;
            await DialogService.ShowErrorAsync("AI request failed", ex.Message);
        }
        finally
        {
            IsAiProcessing = false;
        }
    }

    [RelayCommand]
    private void ApplyAiSummary()
    {
        if (!string.IsNullOrWhiteSpace(AiGeneratedSummary))
        {
            Summary = AiGeneratedSummary;
            AiStatusMessage = "Summary applied";
            AiGeneratedSummary = null;
        }
    }

    [RelayCommand]
    private async Task SuggestAiSkillsAsync()
    {
        if (_services?.AiService is not { IsConfigured: true } ai)
        {
            AiStatusMessage = "Configure the AI endpoint first";
            return;
        }

        IsAiProcessing = true;
        AiStatusMessage = "Suggesting skills...";

        try
        {
            var currentSkills = Skills
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => s.Name)
                .ToList();

            var experiences = Experiences
                .Where(e => !string.IsNullOrWhiteSpace(e.Description))
                .Select(e => e.Description)
                .ToList();

            var result = await ai.SuggestSkillsAsync(JobTitle, currentSkills, experiences);

            if (result.Success && result.Data != null)
            {
                AiSkillSuggestions.Clear();
                foreach (var skill in result.Data)
                {
                    AiSkillSuggestions.Add(skill);
                }
                AiStatusMessage = $"Found {AiSkillSuggestions.Count} skill suggestions";
            }
            else
            {
                AiStatusMessage = result.ErrorMessage ?? "Failed to suggest skills";
                await DialogService.ShowErrorAsync("AI request failed", AiStatusMessage);
            }
        }
        catch (Exception ex)
        {
            AiStatusMessage = ex.Message;
            await DialogService.ShowErrorAsync("AI request failed", ex.Message);
        }
        finally
        {
            IsAiProcessing = false;
        }
    }

    [RelayCommand]
    private void AddSuggestedSkill(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName)) return;

        Skills.Add(new SkillViewModel(new Skill { Name = skillName, Order = Skills.Count }, OnSubViewModelChanged, this));
        AiSkillSuggestions.Remove(skillName);
        OnEditorChanged();
        StatusMessage = $"Added skill: {skillName}";
    }

    [RelayCommand]
    private void DismissSuggestedSkill(string skillName) => AiSkillSuggestions.Remove(skillName);

    // ---------------------------------------------------------------- Tailor to job

    partial void OnJobDescriptionTextChanged(string value)
    {
        if (_isLoadingEditor) return;

        // The posting is part of the resume, but it never shows up in the rendered document, so
        // there is nothing for the preview to redraw.
        CurrentResume.JobDescription = value;
        MarkDirty();
    }

    partial void OnTargetRoleTextChanged(string value)
    {
        if (_isLoadingEditor) return;

        CurrentResume.TargetRole = value;
        MarkDirty();
    }

    [RelayCommand]
    private async Task TailorToJobAsync()
    {
        if (string.IsNullOrWhiteSpace(JobDescriptionText))
        {
            StatusMessage = "Paste a job description first";
            return;
        }

        SyncEditorToResume();

        IsTailoring = true;
        StatusMessage = "Tailoring to the job...";

        try
        {
            var result = await _services.TailoringService.TailorAsync(CurrentResume, JobDescriptionText);

            ApplyKeywordAnalysis(result.Analysis);

            TailoredEdits.Clear();
            foreach (var edit in result.Edits)
            {
                TailoredEdits.Add(new TailoredEditViewModel(edit, DescribeEditTarget(edit)));
            }
            HasTailoredEdits = result.HasAiEdits;

            TailorSuggestedSkills.Clear();
            foreach (var skill in result.SuggestedSkills)
            {
                TailorSuggestedSkills.Add(skill);
            }

            // The keyword analysis stands on its own, so an AI failure is reported beside it rather
            // than replacing it.
            TailorAiError = result.AiError;

            StatusMessage = result.HasAiEdits
                ? $"{KeywordMatchScore}% match - {TailoredEdits.Count} rewrite(s) proposed"
                : $"{KeywordMatchScore}% keyword match";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Tailoring failed", ex.Message);
            StatusMessage = "Tailoring failed";
        }
        finally
        {
            IsTailoring = false;
        }
    }

    private void ApplyKeywordAnalysis(KeywordAnalysisResult result)
    {
        KeywordMatchScore = result.MatchScore;

        MatchedKeywords.Clear();
        foreach (var match in result.MatchedKeywords)
        {
            MatchedKeywords.Add(new KeywordMatchViewModel
            {
                Keyword = match.Keyword,
                Category = match.Category,
                JobCount = match.JobDescriptionCount,
                ResumeCount = match.ResumeCount
            });
        }

        MissingKeywords.Clear();
        foreach (var missing in result.MissingKeywords)
        {
            MissingKeywords.Add(missing);
        }

        KeywordSuggestions.Clear();
        foreach (var suggestion in result.Suggestions)
        {
            KeywordSuggestions.Add(suggestion);
        }

        KeywordWarnings.Clear();
        foreach (var warning in result.Warnings)
        {
            KeywordWarnings.Add(warning);
        }

        HasKeywordAnalysis = true;
    }

    [RelayCommand]
    private void ApplyAcceptedEdits()
    {
        var accepted = TailoredEdits.Where(e => e.Accepted).Select(e => e.Edit).ToList();
        if (accepted.Count == 0)
        {
            StatusMessage = "Tick at least one rewrite to apply";
            return;
        }

        SyncEditorToResume();

        // Undo entries are built from the edits themselves rather than a snapshot, so they survive
        // the editor reload below and revert exactly the fields that were rewritten.
        var undoActions = accepted
            .Select(edit => (IUndoableAction)new TextEditAction(
                $"Tailor.{edit.Target}.{edit.ItemIndex}.{edit.SubIndex}",
                edit.Original,
                edit.Proposed,
                value => SetTailoredValue(edit, value),
                $"Tailor {DescribeEditTarget(edit)}"))
            .ToList();

        var applied = JobTailoringService.Apply(CurrentResume, accepted);

        LoadResumeIntoEditor(CurrentResume);
        MarkDirty();
        UpdatePreviewDebounced();

        _services.UndoRedoManager.RecordAction(
            new CompositeAction($"Apply {applied} tailored edit(s)", undoActions));

        foreach (var editViewModel in TailoredEdits.Where(e => e.Accepted).ToList())
        {
            TailoredEdits.Remove(editViewModel);
        }
        HasTailoredEdits = TailoredEdits.Count > 0;

        StatusMessage = $"Applied {applied} tailored edit(s) - Ctrl+Z to revert";
    }

    /// <summary>
    /// Writes a tailored value back through the editor rather than straight into the model, so undo
    /// and redo update what the user is looking at; the editor then syncs the model as usual.
    /// </summary>
    private void SetTailoredValue(TailoredEdit edit, string value)
    {
        switch (edit.Target)
        {
            case TailoredEditTarget.Summary:
                Summary = value;
                break;

            case TailoredEditTarget.ExperienceDescription
                when edit.ItemIndex >= 0 && edit.ItemIndex < Experiences.Count:
                Experiences[edit.ItemIndex].Description = value;
                break;

            case TailoredEditTarget.ExperienceAchievement
                when edit.ItemIndex >= 0 && edit.ItemIndex < Experiences.Count:
                SetAchievementLine(Experiences[edit.ItemIndex], edit.SubIndex, value);
                break;
        }
    }

    private static void SetAchievementLine(ExperienceViewModel experience, int index, string value)
    {
        experience.Achievements = AchievementLines.ReplaceAt(experience.Achievements, index, value);
    }

    private string DescribeEditTarget(TailoredEdit edit) => edit.Target switch
    {
        TailoredEditTarget.Summary => "summary",
        TailoredEditTarget.ExperienceDescription when edit.ItemIndex < Experiences.Count =>
            $"description for {Experiences[edit.ItemIndex].JobTitle}",
        TailoredEditTarget.ExperienceAchievement when edit.ItemIndex < Experiences.Count =>
            $"bullet {edit.SubIndex + 1} of {Experiences[edit.ItemIndex].JobTitle}",
        _ => "experience"
    };

    [RelayCommand]
    private void AddMissingKeywordAsSkill(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;

        Skills.Add(new SkillViewModel(new Skill { Name = keyword, Order = Skills.Count }, OnSubViewModelChanged, this));
        MissingKeywords.Remove(keyword);
        TailorSuggestedSkills.Remove(keyword);
        OnEditorChanged();
        StatusMessage = $"Added skill: {keyword}";
    }

    [RelayCommand]
    private async Task SaveAsVariantAsync()
    {
        if (string.IsNullOrWhiteSpace(JobDescriptionText))
        {
            StatusMessage = "Paste a job description first";
            return;
        }

        SyncEditorToResume();

        // CreateVariantAsync branches from a persisted row, so an unsaved resume has to land first.
        if (CurrentResume.Id == 0 || IsDirty)
        {
            if (!await SaveAsync(isAutoSave: false))
                return;
        }

        var targetRole = string.IsNullOrWhiteSpace(TargetRoleText) ? JobTitle : TargetRoleText;

        try
        {
            var variant = await _services.Repository.CreateVariantAsync(
                CurrentResume.Id, targetRole.Trim(), JobDescriptionText);

            await LoadSavedResumesAsync();
            StatusMessage = $"Saved variant: {variant.Name}";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Could not save the variant", ex.Message);
        }
    }

    [RelayCommand]
    private void ClearTailoring()
    {
        JobDescriptionText = "";
        TargetRoleText = "";
        KeywordMatchScore = 0;
        MatchedKeywords.Clear();
        MissingKeywords.Clear();
        KeywordSuggestions.Clear();
        KeywordWarnings.Clear();
        TailoredEdits.Clear();
        TailorSuggestedSkills.Clear();
        TailorAiError = null;
        HasTailoredEdits = false;
        HasKeywordAnalysis = false;
    }

    // ---------------------------------------------------------------- Sync

    partial void OnSelectedConflictResolutionChanged(ConflictResolution value)
    {
        if (_services?.SyncService is LocalFolderSyncService sync)
        {
            sync.ConflictResolution = value;
        }
    }

    [RelayCommand]
    private async Task ConfigureSyncAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncFolderPath))
        {
            StatusMessage = "Choose a sync folder first";
            return;
        }

        if (_services?.SyncService == null)
            return;

        var configured = await _services.SyncService.ConfigureAsync(SyncFolderPath);
        IsSyncConfigured = configured;

        if (configured)
        {
            OnSelectedConflictResolutionChanged(SelectedConflictResolution);
            SyncStatusText = "Configured - ready to sync";
            StatusMessage = "Sync configured";
            await RefreshRemoteResumesAsync();
        }
        else
        {
            SyncStatusText = "Configuration failed";
            await DialogService.ShowErrorAsync("Sync setup failed",
                $"Could not use '{SyncFolderPath}' as a sync folder. Check that the path is valid and writable.");
        }
    }

    [RelayCommand]
    private async Task BrowseSyncFolderAsync()
    {
        try
        {
            var topLevel = MainWindow;
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new Avalonia.Platform.Storage.FolderPickerOpenOptions
                {
                    Title = "Select Sync Folder",
                    AllowMultiple = false
                });

            if (folders.Count > 0)
            {
                SyncFolderPath = folders[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Could not open folder picker", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SyncAllAsync()
    {
        if (_services?.SyncService == null || !IsSyncConfigured)
        {
            StatusMessage = "Sync is not configured";
            return;
        }

        if (IsDirty && !await SaveAsync(isAutoSave: false))
            return;

        IsSyncing = true;
        SyncStatusText = "Syncing...";

        try
        {
            var result = await _services.SyncService.SyncAllAsync();
            await ApplySyncResultAsync(result);
        }
        catch (Exception ex)
        {
            SyncStatusText = "Sync failed";
            await DialogService.ShowErrorAsync("Sync failed", ex.Message);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task SyncCurrentResumeAsync()
    {
        if (_services?.SyncService == null || !IsSyncConfigured)
        {
            StatusMessage = "Sync is not configured";
            return;
        }

        // A resume that has never been saved has no id for the sync service to work with.
        if (CurrentResume.Id == 0 || IsDirty)
        {
            if (!await SaveAsync(isAutoSave: false))
                return;
        }

        IsSyncing = true;
        SyncStatusText = "Syncing...";

        try
        {
            var result = await _services.SyncService.SyncResumeAsync(CurrentResume.Id);
            await ApplySyncResultAsync(result);
        }
        catch (Exception ex)
        {
            SyncStatusText = "Sync failed";
            await DialogService.ShowErrorAsync("Sync failed", ex.Message);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async Task ApplySyncResultAsync(SyncResult result)
    {
        SyncUploadedCount = result.UploadedCount;
        SyncDownloadedCount = result.DownloadedCount;
        HasSyncResult = true;

        SyncConflicts.Clear();
        foreach (var conflict in result.Conflicts)
        {
            SyncConflicts.Add(new SyncConflictViewModel
            {
                ResumeName = conflict.ResumeName,
                LocalModified = conflict.LocalModified,
                RemoteModified = conflict.RemoteModified
            });
        }

        SyncStatusText = result.Message ?? result.Status.ToString();
        StatusMessage = $"Sync: {SyncStatusText}";

        await LoadSavedResumesAsync();
        await RefreshRemoteResumesAsync();

        if (result.Errors.Count > 0)
        {
            await DialogService.ShowErrorAsync("Sync finished with errors", string.Join("\n", result.Errors));
        }

        // A download can overwrite the row the editor is showing.
        if (result.DownloadedCount > 0 && CurrentResume.Id != 0)
        {
            await ReloadCurrentResumeAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshRemoteResumesAsync()
    {
        if (_services?.SyncService == null || !IsSyncConfigured)
            return;

        try
        {
            var remotes = await _services.SyncService.ListRemoteAsync();
            RemoteResumes.Clear();

            foreach (var remote in remotes)
            {
                RemoteResumes.Add(new RemoteResumeViewModel
                {
                    Name = remote.Name,
                    Path = remote.Path,
                    LastModified = remote.LastModified,
                    Size = remote.Size
                });
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Could not list remote resumes", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ImportRemoteResumeAsync(string remotePath)
    {
        if (_services?.SyncService == null || string.IsNullOrEmpty(remotePath))
            return;

        if (!await ConfirmDiscardChangesAsync())
            return;

        IsSyncing = true;

        try
        {
            var json = await _services.SyncService.DownloadAsync(remotePath);
            if (json == null)
            {
                await DialogService.ShowErrorAsync("Import failed", $"Could not read '{remotePath}' from the sync folder.");
                return;
            }

            var resume = JsonSerializer.Deserialize<Resume>(json);
            if (resume == null)
            {
                await DialogService.ShowErrorAsync("Import failed", $"'{remotePath}' is not a valid resume file.");
                return;
            }

            AdoptImportedResume(resume, preserveSyncId: true);
            StatusMessage = "Resume imported from the sync folder";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Import failed", ex.Message);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    // ---------------------------------------------------------------- Preview

    private void UpdatePreviewDebounced()
    {
        if (_previewDebounceTimer == null)
        {
            _previewDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _previewDebounceTimer.Tick += (_, _) =>
            {
                _previewDebounceTimer!.Stop();
                _ = UpdatePreviewAsync();
            };
        }

        _previewDebounceTimer.Stop();
        _previewDebounceTimer.Start();
    }

    private async Task UpdatePreviewAsync()
    {
        if (_services == null) return;

        SyncEditorToResume();

        var templateId = SelectedTemplate?.Id ?? "modern";
        string json;

        try
        {
            json = JsonSerializer.Serialize(CurrentResume);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Preview failed", ex.Message);
            return;
        }

        var signature = $"{templateId}|{json}";
        if (signature == _renderedSignature)
            return;

        _previewCts?.Cancel();
        _previewCts?.Dispose();
        var cts = new CancellationTokenSource();
        _previewCts = cts;

        try
        {
            // Render off a snapshot: the editor keeps mutating CurrentResume while this runs.
            var snapshot = JsonSerializer.Deserialize<Resume>(json)!;
            var registry = _services.TemplateRegistry;

            var pages = await Task.Run(() =>
            {
                var template = registry.GetTemplateOrDefault(templateId);
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

            var superseded = PreviewPages.ToList();

            PreviewPages = rendered;
            PreviewImage = rendered.FirstOrDefault();
            _renderedSignature = signature;

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
            StatusMessage = "Preview failed";
            await DialogService.ShowErrorAsync("Preview failed", ex.Message);
        }
    }

    // ---------------------------------------------------------------- Save / open / delete

    private void SetupAutoSave()
    {
        // A DispatcherTimer keeps the save on the UI thread, where the bound collections live.
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _autoSaveTimer.Tick += async (_, _) =>
        {
            await AutoSaveAsync();
            await AutoSaveCoverLetterAsync();
        };
        _autoSaveTimer.Start();
    }

    private async Task AutoSaveAsync()
    {
        if (!IsDirty || IsSaving)
            return;

        // Never let the timer create a record for an untouched blank resume.
        if (CurrentResume.Id == 0 && !HasMeaningfulContent())
            return;

        await SaveAsync(isAutoSave: true);
    }

    [RelayCommand]
    private async Task SaveCurrentResumeAsync() => await SaveAsync(isAutoSave: false);

    private async Task<bool> SaveAsync(bool isAutoSave)
    {
        if (isAutoSave)
        {
            if (!await _saveGate.WaitAsync(0))
                return false;
        }
        else
        {
            await _saveGate.WaitAsync();
        }

        try
        {
            SyncEditorToResume();

            IsSaving = true;
            UpdateSaveState();

            if (CurrentResume.Id == 0)
            {
                await _services.Repository.CreateAsync(CurrentResume);
            }
            else
            {
                await _services.Repository.UpdateAsync(CurrentResume);
            }

            IsDirty = false;
            _lastSavedAt = DateTime.Now;

            await LoadSavedResumesAsync();
            StatusMessage = isAutoSave
                ? $"Auto-saved at {_lastSavedAt:HH:mm:ss}"
                : $"Saved at {_lastSavedAt:HH:mm:ss}";
            return true;
        }
        catch (ResumeConcurrencyException ex)
        {
            var reload = await DialogService.ConfirmAsync(
                "Resume changed elsewhere",
                $"{ex.Message}\n\nReload the saved version? Your unsaved changes to this resume will be lost.",
                "Reload saved version",
                "Keep editing");

            if (reload)
            {
                await ReloadCurrentResumeAsync();
            }
            return false;
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Save failed", ex.Message);
            return false;
        }
        finally
        {
            IsSaving = false;
            UpdateSaveState();
            _saveGate.Release();
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        var name = await DialogService.PromptAsync("Save As", "Name for the new resume:", ResumeName);
        if (name == null) return;

        try
        {
            SyncEditorToResume();

            var copy = ResumeRepository.DeepClone(CurrentResume);
            ResumeRepository.ResetIdentity(copy);
            copy.SyncId = Guid.NewGuid();
            copy.Name = name;

            await _services.Repository.CreateAsync(copy);

            _services.UndoRedoManager.Clear();
            CurrentResume = copy;
            LoadResumeIntoEditor(copy);

            IsDirty = false;
            _lastSavedAt = DateTime.Now;
            UpdateSaveState();

            await LoadSavedResumesAsync();
            StatusMessage = $"Saved as: {name}";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Save As failed", ex.Message);
        }
    }

    private async Task ReloadCurrentResumeAsync()
    {
        if (CurrentResume.Id == 0) return;

        var reloaded = await _services.Repository.GetByIdAsync(CurrentResume.Id);
        if (reloaded == null)
        {
            StatusMessage = "This resume no longer exists";
            return;
        }

        _services.UndoRedoManager.Clear();
        CurrentResume = reloaded;
        LoadResumeIntoEditor(reloaded);
        IsDirty = false;
        _lastSavedAt = DateTime.Now;
        UpdateSaveState();
        UpdatePreviewDebounced();
    }

    /// <summary>
    /// Returns false when the user backs out; callers must abandon the operation in that case.
    /// </summary>
    public async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!IsDirty)
            return true;

        var choice = await DialogService.ConfirmUnsavedChangesAsync(ResumeName);
        return choice switch
        {
            UnsavedChangesChoice.Save => await SaveAsync(isAutoSave: false),
            UnsavedChangesChoice.Discard => true,
            _ => false
        };
    }

    [RelayCommand]
    private async Task CreateNewResumeAsync()
    {
        if (!await ConfirmDiscardChangesAsync())
            return;

        ShowResumeList = false;
        ResetToNewResume();
    }

    [RelayCommand]
    private async Task OpenResumeListAsync()
    {
        await LoadSavedResumesAsync();
        ShowResumeList = true;
    }

    [RelayCommand]
    private void CloseResumeList() => ShowResumeList = false;

    [RelayCommand]
    private async Task LoadResumeAsync(Resume resume)
    {
        if (!await ConfirmDiscardChangesAsync())
            return;

        try
        {
            var loaded = await _services.Repository.GetByIdAsync(resume.Id);
            if (loaded == null)
            {
                await DialogService.ShowErrorAsync("Open failed", $"'{resume.Name}' no longer exists.");
                await LoadSavedResumesAsync();
                return;
            }

            _services.UndoRedoManager.Clear();
            CurrentResume = loaded;
            LoadResumeIntoEditor(loaded);
            IsDirty = false;
            _lastSavedAt = null;
            UpdateSaveState();
            UpdatePreviewDebounced();
            ShowResumeList = false;
            StatusMessage = $"Opened: {loaded.Name}";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Open failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteResumeAsync(Resume resume)
    {
        var confirmed = await DialogService.ConfirmAsync(
            "Delete resume",
            $"Delete \"{resume.Name}\"? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            await _services.Repository.DeleteAsync(resume.Id);

            // The editor is showing the row that just went away; keep the content, drop the identity.
            if (CurrentResume.Id == resume.Id)
            {
                CurrentResume.Id = 0;
                MarkDirty();
            }

            await LoadSavedResumesAsync();
            StatusMessage = $"Deleted: {resume.Name}";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Delete failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DuplicateResumeAsync(Resume resume)
    {
        try
        {
            var copy = await _services.Repository.DuplicateAsync(resume.Id);
            await LoadSavedResumesAsync();
            StatusMessage = $"Duplicated: {copy.Name}";
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Duplicate failed", ex.Message);
        }
    }

    [RelayCommand]
    private void SelectTemplate(TemplateInfo template)
    {
        SelectedTemplate = template;
        ShowTemplateGallery = false;
        StatusMessage = $"Template: {template.Name}";
    }

    // ---------------------------------------------------------------- Export / import

    [RelayCommand]
    private async Task ExportAsync(string format)
    {
        try
        {
            var topLevel = MainWindow;
            if (topLevel == null) return;

            SyncEditorToResume();

            var exporter = _services.ExportService.GetExporter(format);
            if (exporter == null)
            {
                await DialogService.ShowErrorAsync("Export failed", $"Unknown export format: {format}");
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = $"Export as {format}",
                SuggestedFileName = BuildExportFileName(),
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

            await _services.ExportService.ExportToFileAsync(CurrentResume, format, filePath);
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

    /// <summary>Falls back through full name, resume name, then a constant, so the file is never nameless.</summary>
    private string BuildExportFileName()
    {
        var candidate = CurrentResume.PersonalInfo.FullName;

        if (string.IsNullOrWhiteSpace(candidate))
            candidate = CurrentResume.Name;

        if (string.IsNullOrWhiteSpace(candidate) || candidate == DefaultResumeName)
            return "Resume";

        var sanitized = SanitizeFileName(candidate.Trim()).Replace(" ", "_");
        return string.IsNullOrWhiteSpace(sanitized) ? "Resume" : $"{sanitized}_Resume";
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    [RelayCommand]
    private Task ImportJsonResumeAsync() => ImportAsync(
        "JSON Resume",
        new Avalonia.Platform.Storage.FilePickerFileType("JSON Resume")
        {
            Patterns = new[] { "*.json" },
            MimeTypes = new[] { "application/json" }
        });

    [RelayCommand]
    private Task ImportLinkedInAsync() => ImportAsync(
        "LinkedIn",
        new Avalonia.Platform.Storage.FilePickerFileType("LinkedIn Export (ZIP)")
        {
            Patterns = new[] { "*.zip" },
            MimeTypes = new[] { "application/zip" }
        });

    [RelayCommand]
    private Task ImportPdfAsync() => ImportAsync(
        "PDF",
        new Avalonia.Platform.Storage.FilePickerFileType("PDF Resume")
        {
            Patterns = new[] { "*.pdf" },
            MimeTypes = new[] { "application/pdf" }
        });

    private async Task ImportAsync(string importerName, Avalonia.Platform.Storage.FilePickerFileType fileType)
    {
        try
        {
            var topLevel = MainWindow;
            if (topLevel == null) return;

            if (!await ConfirmDiscardChangesAsync())
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = $"Import {importerName}",
                AllowMultiple = false,
                FileTypeFilter = new[] { fileType }
            });

            if (files.Count == 0) return;

            IsLoading = true;
            StatusMessage = $"Importing {importerName}...";

            await using var stream = await files[0].OpenReadAsync();
            var result = await _services.ExportService.ImportAsync(stream, importerName);

            if (!result.Success || result.Data == null)
            {
                await DialogService.ShowErrorAsync("Import failed", result.ErrorMessage ?? "The file could not be read.");
                StatusMessage = "Import failed";
                return;
            }

            AdoptImportedResume(result.Data);

            StatusMessage = result.Warnings.Any()
                ? $"Imported with {result.Warnings.Count} warning(s) - review the result"
                : "Imported successfully";

            if (result.Warnings.Any())
            {
                await DialogService.ShowErrorAsync("Imported with warnings", string.Join("\n", result.Warnings));
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync("Import failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// An imported resume carries ids from whichever database exported it. Clearing them keeps the
    /// next save from overwriting the unrelated local row that happens to own those ids.
    /// </summary>
    /// <param name="preserveSyncId">
    /// True when the file came from our own sync folder: it is the same resume, so keeping its
    /// SyncId lets sync recognize it. A file from anywhere else is a new resume and must get a
    /// fresh SyncId, or it would collide with the resume it was exported from.
    /// </param>
    private void AdoptImportedResume(Resume imported, bool preserveSyncId = false)
    {
        var syncId = imported.SyncId;

        ResumeRepository.ResetIdentity(imported);

        if (preserveSyncId && syncId != Guid.Empty)
        {
            imported.SyncId = syncId;
        }

        imported.SectionOrder.EnsureAllSectionsPresent();

        if (string.IsNullOrWhiteSpace(imported.Name))
            imported.Name = DefaultResumeName;

        _services.UndoRedoManager.Clear();
        CurrentResume = imported;
        LoadResumeIntoEditor(imported);

        IsDirty = true;
        _lastSavedAt = null;
        UpdateSaveState();
        UpdatePreviewDebounced();
    }

    // ---------------------------------------------------------------- Collections

    [RelayCommand]
    private void AddExperience() => ExecuteAdd(Experiences,
        new ExperienceViewModel(new Experience { Order = Experiences.Count }, OnSubViewModelChanged, this), "Experience");

    [RelayCommand]
    private void RemoveExperience(ExperienceViewModel exp) => ExecuteRemove(Experiences, exp, "Experience");

    [RelayCommand]
    private void AddEducation() => ExecuteAdd(EducationList,
        new EducationViewModel(new Education { Order = EducationList.Count }, OnSubViewModelChanged, this), "Education");

    [RelayCommand]
    private void RemoveEducation(EducationViewModel edu) => ExecuteRemove(EducationList, edu, "Education");

    [RelayCommand]
    private void AddSkill() => ExecuteAdd(Skills,
        new SkillViewModel(new Skill { Order = Skills.Count }, OnSubViewModelChanged, this), "Skill");

    [RelayCommand]
    private void RemoveSkill(SkillViewModel skill) => ExecuteRemove(Skills, skill, "Skill");

    [RelayCommand]
    private void AddLanguage() => ExecuteAdd(Languages,
        new LanguageViewModel(new Language { Order = Languages.Count }, OnSubViewModelChanged, this), "Language");

    [RelayCommand]
    private void RemoveLanguage(LanguageViewModel lang) => ExecuteRemove(Languages, lang, "Language");

    [RelayCommand]
    private void AddCertification() => ExecuteAdd(Certifications,
        new CertificationViewModel(new Certification { Order = Certifications.Count }, OnSubViewModelChanged, this), "Certification");

    [RelayCommand]
    private void RemoveCertification(CertificationViewModel cert) => ExecuteRemove(Certifications, cert, "Certification");

    [RelayCommand]
    private void AddProject() => ExecuteAdd(Projects,
        new ProjectViewModel(new Project { Order = Projects.Count }, OnSubViewModelChanged, this), "Project");

    [RelayCommand]
    private void RemoveProject(ProjectViewModel proj) => ExecuteRemove(Projects, proj, "Project");

    [RelayCommand]
    private void AddCustomSection() => ExecuteAdd(CustomSections,
        new CustomSectionViewModel(new CustomSection { Title = "New Section", Order = CustomSections.Count }, OnSubViewModelChanged, this),
        "Custom Section");

    [RelayCommand]
    private void RemoveCustomSection(CustomSectionViewModel section) => ExecuteRemove(CustomSections, section, "Custom Section");

    private void ExecuteAdd<T>(ObservableCollection<T> collection, T item, string description) =>
        _services?.UndoRedoManager?.Execute(
            CollectionChangeAction<T>.Add(collection, item, -1, description, OnSubViewModelChanged));

    private void ExecuteRemove<T>(ObservableCollection<T> collection, T item, string description)
    {
        var index = collection.IndexOf(item);
        if (index < 0) return;

        _services?.UndoRedoManager?.Execute(
            CollectionChangeAction<T>.Remove(collection, item, index, description, OnSubViewModelChanged));
    }

    private void ExecuteMove<T>(ObservableCollection<T> collection, T item, int offset, string description)
    {
        var index = collection.IndexOf(item);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= collection.Count) return;

        _services?.UndoRedoManager?.Execute(
            CollectionChangeAction<T>.Move(collection, index, target, description, OnSubViewModelChanged));
    }

    [RelayCommand]
    private void MoveExperienceUp(ExperienceViewModel exp) => ExecuteMove(Experiences, exp, -1, "Experience");

    [RelayCommand]
    private void MoveExperienceDown(ExperienceViewModel exp) => ExecuteMove(Experiences, exp, 1, "Experience");

    [RelayCommand]
    private void MoveEducationUp(EducationViewModel edu) => ExecuteMove(EducationList, edu, -1, "Education");

    [RelayCommand]
    private void MoveEducationDown(EducationViewModel edu) => ExecuteMove(EducationList, edu, 1, "Education");

    [RelayCommand]
    private void MoveSkillUp(SkillViewModel skill) => ExecuteMove(Skills, skill, -1, "Skill");

    [RelayCommand]
    private void MoveSkillDown(SkillViewModel skill) => ExecuteMove(Skills, skill, 1, "Skill");

    [RelayCommand]
    private void MoveLanguageUp(LanguageViewModel lang) => ExecuteMove(Languages, lang, -1, "Language");

    [RelayCommand]
    private void MoveLanguageDown(LanguageViewModel lang) => ExecuteMove(Languages, lang, 1, "Language");

    [RelayCommand]
    private void MoveCertificationUp(CertificationViewModel cert) => ExecuteMove(Certifications, cert, -1, "Certification");

    [RelayCommand]
    private void MoveCertificationDown(CertificationViewModel cert) => ExecuteMove(Certifications, cert, 1, "Certification");

    [RelayCommand]
    private void MoveProjectUp(ProjectViewModel proj) => ExecuteMove(Projects, proj, -1, "Project");

    [RelayCommand]
    private void MoveProjectDown(ProjectViewModel proj) => ExecuteMove(Projects, proj, 1, "Project");

    // ---------------------------------------------------------------- Zoom

    [RelayCommand]
    private void ZoomIn() => PreviewZoom = Math.Min(PreviewZoom + 0.1, 2.0);

    [RelayCommand]
    private void ZoomOut() => PreviewZoom = Math.Max(PreviewZoom - 0.1, 0.5);

    [RelayCommand]
    private void ZoomReset() => PreviewZoom = 1.0;

    private static Avalonia.Controls.Window? MainWindow =>
        (Avalonia.Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
