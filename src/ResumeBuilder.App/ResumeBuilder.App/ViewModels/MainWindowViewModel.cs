using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using ResumeBuilder.Templates;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using Avalonia.Media.Imaging;

namespace ResumeBuilder.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private System.Timers.Timer? _autoSaveTimer;
    private System.Timers.Timer? _previewDebounceTimer;
    private bool _isLoadingEditor;

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

    [ObservableProperty]
    private int _selectedEditorTab;

    [ObservableProperty]
    private bool _showTemplateGallery;

    [ObservableProperty]
    private bool _showResumeList;

    [ObservableProperty]
    private double _previewZoom = 1.0;

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
    private string _accentColor = "#2563eb";

    [ObservableProperty]
    private string _secondaryColor = "#64748b";

    [ObservableProperty]
    private string _textColor = "#1f2937";

    [ObservableProperty]
    private string _headingColor = "#111827";

    [ObservableProperty]
    private string _selectedFontFamily = "Arial";

    [ObservableProperty]
    private string _selectedHeadingFontFamily = "Arial";

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
    private Avalonia.Media.Color _accentColorValue = Avalonia.Media.Color.Parse("#2563eb");

    private bool _isSyncingColor;

    partial void OnAccentColorValueChanged(Avalonia.Media.Color value)
    {
        if (_isSyncingColor) return;

        _isSyncingColor = true;
        try
        {
            var newHex = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
            if (AccentColor != newHex)
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

    private static bool TryParseHexColor(string hex, out Avalonia.Media.Color color)
    {
        color = default;
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length != 7)
            return false;

        try
        {
            color = Avalonia.Media.Color.Parse(hex);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Avalonia.Data.Converters.FuncValueConverter<string, Avalonia.Media.IBrush> HexToColorConverter { get; } =
        new(hex =>
        {
            if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length != 7)
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Blue);

            try
            {
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(hex));
            }
            catch
            {
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Blue);
            }
        });

    [RelayCommand]
    private void ToggleAccentColorPicker()
    {
        ShowAccentColorPicker = !ShowAccentColorPicker;
    }

    public string[] AvailableFonts => TemplateSettings.AvailableFonts;
    public string[] PresetColors => TemplateSettings.PresetColors;

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

    private bool _suppressUndoRecording;

    // AI Features
    [ObservableProperty]
    private bool _showAiPanel;

    [ObservableProperty]
    private bool _isAiConfigured;

    [ObservableProperty]
    private string _aiApiKey = "";

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

    // Keyword Optimization
    [ObservableProperty]
    private bool _showKeywordPanel;

    [ObservableProperty]
    private string _jobDescriptionText = "";

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
            CreateNewResume();
            InitializeTheme();
            InitializeUndoRedo();
        }
    }

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

        manager.ActionUndone += _ =>
        {
            UpdatePreviewDebounced();
            StatusMessage = $"Undo: {manager.NextRedoDescription}";
        };

        manager.ActionRedone += _ =>
        {
            UpdatePreviewDebounced();
            StatusMessage = $"Redo: {manager.NextUndoDescription}";
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

    private void RecordPropertyChange<T>(string propertyName, T oldValue, T newValue, Action<T?> setter)
    {
        if (_suppressUndoRecording || _services?.UndoRedoManager == null)
            return;

        if (_services.UndoRedoManager.IsExecutingAction)
            return;

        // Don't record if values are the same
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            return;

        var action = new PropertyChangeAction<T>(
            setter,
            oldValue,
            newValue,
            $"Change {propertyName}");

        _services.UndoRedoManager.RecordAction(action);
    }

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

    // Validation
    private readonly EmailRule _emailValidator = new();
    private readonly PhoneRule _phoneValidator = new();
    private readonly UrlRule _urlValidator = new();
    private readonly LinkedInRule _linkedInValidator = new();

    partial void OnEmailChanged(string value)
    {
        ValidateEmail(value);
        SyncEditorToResume();
        UpdatePreviewDebounced();
    }

    partial void OnPhoneChanged(string value)
    {
        ValidatePhone(value);
        SyncEditorToResume();
        UpdatePreviewDebounced();
    }

    partial void OnWebsiteChanged(string value)
    {
        ValidateWebsite(value);
        SyncEditorToResume();
        UpdatePreviewDebounced();
    }

    partial void OnLinkedInChanged(string value)
    {
        ValidateLinkedIn(value);
        SyncEditorToResume();
        UpdatePreviewDebounced();
    }

    private void ValidateEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            EmailError = null;
        }
        else
        {
            var result = _emailValidator.Validate(value);
            EmailError = result.IsValid ? null : result.ErrorMessage;
        }
        UpdateValidationState();
    }

    private void ValidatePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            PhoneError = null;
        }
        else
        {
            var result = _phoneValidator.Validate(value);
            PhoneError = result.IsValid ? null : result.ErrorMessage;
        }
        UpdateValidationState();
    }

    private void ValidateWebsite(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            WebsiteError = null;
        }
        else
        {
            var result = _urlValidator.Validate(value);
            WebsiteError = result.IsValid ? null : result.ErrorMessage;
        }
        UpdateValidationState();
    }

    private void ValidateLinkedIn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            LinkedInError = null;
        }
        else
        {
            var result = _linkedInValidator.Validate(value);
            LinkedInError = result.IsValid ? null : result.ErrorMessage;
        }
        UpdateValidationState();
    }

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

    // Photo
    private readonly ImageService _imageService = new();

    [RelayCommand]
    private async Task UploadPhotoAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

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
                UpdatePreviewDebounced();
                StatusMessage = "Photo uploaded successfully";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error uploading photo: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        CurrentResume.PersonalInfo.Photo = null;
        PhotoPreview = null;
        HasPhoto = false;
        UpdatePreviewDebounced();
        StatusMessage = "Photo removed";
    }

    private void LoadPhotoPreview()
    {
        if (CurrentResume.PersonalInfo.Photo != null && CurrentResume.PersonalInfo.Photo.Length > 0)
        {
            PhotoPreview = _imageService.BytesToBitmap(CurrentResume.PersonalInfo.Photo);
            HasPhoto = true;
        }
        else
        {
            PhotoPreview = null;
            HasPhoto = false;
        }
    }

    private void LoadTemplates()
    {
        Templates.Clear();
        foreach (var template in _services.TemplateRegistry.GetAllTemplateInfos())
        {
            Templates.Add(template);
        }
        SelectedTemplate = Templates.FirstOrDefault(t => t.Id == "modern") ?? Templates.FirstOrDefault();
    }

    private async Task LoadSavedResumesAsync()
    {
        var resumes = await _services.Repository.GetAllAsync();
        SavedResumes.Clear();
        foreach (var resume in resumes)
        {
            SavedResumes.Add(resume);
        }
    }

    private void SetupAutoSave()
    {
        _autoSaveTimer = new System.Timers.Timer(30000); // 30 seconds
        _autoSaveTimer.Elapsed += async (s, e) => await SaveCurrentResumeAsync();
        _autoSaveTimer.Start();
    }

    [RelayCommand]
    private void CreateNewResume()
    {
        CurrentResume = new Resume
        {
            Name = "Untitled Resume",
            SelectedTemplateId = SelectedTemplate?.Id ?? "modern"
        };

        LoadResumeIntoEditor(CurrentResume);
        UpdatePreviewDebounced();
        StatusMessage = "New resume created";
    }

    private void LoadResumeIntoEditor(Resume resume)
    {
        _isLoadingEditor = true;
        try
        {
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

        Experiences.Clear();
        foreach (var exp in resume.Experiences)
        {
            Experiences.Add(new ExperienceViewModel(exp, OnSubViewModelChanged));
        }

        EducationList.Clear();
        foreach (var edu in resume.EducationList)
        {
            EducationList.Add(new EducationViewModel(edu, OnSubViewModelChanged));
        }

        Skills.Clear();
        foreach (var skill in resume.Skills)
        {
            Skills.Add(new SkillViewModel(skill, OnSubViewModelChanged));
        }

        Languages.Clear();
        foreach (var lang in resume.Languages)
        {
            Languages.Add(new LanguageViewModel(lang, OnSubViewModelChanged));
        }

        Certifications.Clear();
        foreach (var cert in resume.Certifications)
        {
            Certifications.Add(new CertificationViewModel(cert, OnSubViewModelChanged));
        }

        Projects.Clear();
        foreach (var proj in resume.Projects)
        {
            Projects.Add(new ProjectViewModel(proj, OnSubViewModelChanged));
        }

        SelectedTemplate = Templates.FirstOrDefault(t => t.Id == resume.SelectedTemplateId) ?? Templates.FirstOrDefault();

        // Load photo preview
        LoadPhotoPreview();

        // Load customization settings
        LoadCustomizationFromResume();

        // Load section ordering
        LoadSectionsFromResume();
        }
        finally
        {
            _isLoadingEditor = false;
        }
    }

    private void SyncEditorToResume()
    {
        if (_isLoadingEditor) return;

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

        CurrentResume.Experiences = Experiences.Select(e => e.ToModel()).ToList();
        CurrentResume.EducationList = EducationList.Select(e => e.ToModel()).ToList();
        CurrentResume.Skills = Skills.Select(s => s.ToModel()).ToList();
        CurrentResume.Languages = Languages.Select(l => l.ToModel()).ToList();
        CurrentResume.Certifications = Certifications.Select(c => c.ToModel()).ToList();
        CurrentResume.Projects = Projects.Select(p => p.ToModel()).ToList();
    }

    private void OnSubViewModelChanged()
    {
        SyncEditorToResume();
        UpdatePreviewDebounced();
    }

    partial void OnFirstNameChanged(string value) { SyncEditorToResume(); UpdatePreviewDebounced(); }
    partial void OnLastNameChanged(string value) { SyncEditorToResume(); UpdatePreviewDebounced(); }
    partial void OnJobTitleChanged(string value) { SyncEditorToResume(); UpdatePreviewDebounced(); }
    partial void OnCityChanged(string value) { SyncEditorToResume(); UpdatePreviewDebounced(); }
    partial void OnCountryChanged(string value) { SyncEditorToResume(); UpdatePreviewDebounced(); }
    partial void OnGitHubChanged(string value) { SyncEditorToResume(); UpdatePreviewDebounced(); }
    partial void OnSelectedTemplateChanged(TemplateInfo? value) => UpdatePreviewDebounced();

    // Customization property change handlers
    partial void OnAccentColorChanged(string value) { SyncCustomizationToResume(); UpdatePreviewDebounced(); SyncAccentColorToColorPicker(); }
    partial void OnSecondaryColorChanged(string value) { SyncCustomizationToResume(); UpdatePreviewDebounced(); }
    partial void OnTextColorChanged(string value) { SyncCustomizationToResume(); UpdatePreviewDebounced(); }
    partial void OnHeadingColorChanged(string value) { SyncCustomizationToResume(); UpdatePreviewDebounced(); }
    partial void OnSelectedFontFamilyChanged(string value) { SyncCustomizationToResume(); UpdatePreviewDebounced(); }
    partial void OnSelectedHeadingFontFamilyChanged(string value) { SyncCustomizationToResume(); UpdatePreviewDebounced(); }
    partial void OnFontSizeScaleChanged(float value) { SyncCustomizationToResume(); UpdatePreviewDebounced(); }
    partial void OnLineSpacingChanged(float value) { SyncCustomizationToResume(); UpdatePreviewDebounced(); }
    partial void OnSectionSpacingChanged(float value) { SyncCustomizationToResume(); UpdatePreviewDebounced(); }
    partial void OnPageMarginChanged(float value) { SyncCustomizationToResume(); UpdatePreviewDebounced(); }

    private void SyncCustomizationToResume()
    {
        CurrentResume.TemplateSettings.AccentColor = AccentColor;
        CurrentResume.TemplateSettings.SecondaryColor = SecondaryColor;
        CurrentResume.TemplateSettings.TextColor = TextColor;
        CurrentResume.TemplateSettings.HeadingColor = HeadingColor;
        CurrentResume.TemplateSettings.FontFamily = SelectedFontFamily;
        CurrentResume.TemplateSettings.HeadingFontFamily = SelectedHeadingFontFamily;
        CurrentResume.TemplateSettings.FontSizeScale = FontSizeScale;
        CurrentResume.TemplateSettings.LineSpacing = LineSpacing;
        CurrentResume.TemplateSettings.SectionSpacing = SectionSpacing;
        CurrentResume.TemplateSettings.PageMargin = PageMargin;

        // Also update legacy properties for backwards compatibility
        CurrentResume.AccentColor = AccentColor;
        CurrentResume.FontFamily = SelectedFontFamily;
    }

    private void LoadCustomizationFromResume()
    {
        var settings = CurrentResume.TemplateSettings ?? new TemplateSettings();
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
    }

    [RelayCommand]
    private void SetAccentColor(string color)
    {
        AccentColor = color;
    }

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
        StatusMessage = "Customization reset to defaults";
    }

    // Section Ordering Methods
    private void LoadSectionsFromResume()
    {
        Sections.Clear();
        var sectionOrder = CurrentResume.SectionOrder ?? new SectionOrder();

        for (int i = 0; i < sectionOrder.OrderedSections.Count; i++)
        {
            var type = sectionOrder.OrderedSections[i];
            var isVisible = sectionOrder.IsSectionVisible(type);
            Sections.Add(new SectionViewModel(type, isVisible, i, OnSectionChanged));
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
        SyncSectionsToResume();
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
            SyncSectionsToResume();
            UpdatePreviewDebounced();
        }
    }

    [RelayCommand]
    private void MoveSectionDown(SectionViewModel section)
    {
        var index = Sections.IndexOf(section);
        if (index < Sections.Count - 1)
        {
            Sections.Move(index, index + 1);
            UpdateSectionOrders();
            SyncSectionsToResume();
            UpdatePreviewDebounced();
        }
    }

    [RelayCommand]
    private void ResetSectionOrder()
    {
        CurrentResume.SectionOrder = new SectionOrder();
        LoadSectionsFromResume();
        UpdatePreviewDebounced();
        StatusMessage = "Section order reset to default";
    }

    private void UpdateSectionOrders()
    {
        for (int i = 0; i < Sections.Count; i++)
        {
            Sections[i].Order = i;
        }
    }

    // Spell Check Methods
    private System.Timers.Timer? _spellCheckTimer;

    private void CheckSpellingDebounced()
    {
        if (!IsSpellCheckEnabled || _services?.SpellChecker == null)
            return;

        _spellCheckTimer?.Stop();
        _spellCheckTimer?.Dispose();

        _spellCheckTimer = new System.Timers.Timer(500);
        _spellCheckTimer.Elapsed += (s, e) =>
        {
            _spellCheckTimer?.Stop();
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(CheckSummarySpelling);
        };
        _spellCheckTimer.AutoReset = false;
        _spellCheckTimer.Start();
    }

    private void CheckSummarySpelling()
    {
        if (!IsSpellCheckEnabled || _services?.SpellChecker == null || !_services.SpellChecker.IsInitialized)
        {
            SummarySpellErrorCount = 0;
            SummarySpellResult = null;
            return;
        }

        var result = _services.SpellChecker.CheckText(Summary);
        SummarySpellResult = result;
        SummarySpellErrorCount = result.ErrorCount;
    }

    [RelayCommand]
    private void ShowSpellingSuggestions()
    {
        if (SummarySpellResult == null || !SummarySpellResult.HasErrors)
            return;

        SpellSuggestions.Clear();
        foreach (var misspelled in SummarySpellResult.MisspelledWords.Take(10))
        {
            SpellSuggestions.Add(new SpellSuggestionViewModel(
                misspelled.Word,
                misspelled.Suggestions,
                word => ApplySpellingSuggestion(misspelled.Word, word),
                word => AddToPersonalDictionary(word)));
        }
        ShowSpellSuggestions = true;
    }

    private void ApplySpellingSuggestion(string misspelled, string replacement)
    {
        Summary = Summary.Replace(misspelled, replacement);
        ShowSpellSuggestions = false;
        CheckSummarySpelling();
    }

    private void AddToPersonalDictionary(string word)
    {
        _services?.SpellChecker?.AddToPersonalDictionary(word);
        CheckSummarySpelling();
        StatusMessage = $"Added '{word}' to personal dictionary";
    }

    [RelayCommand]
    private void CloseSpellSuggestions()
    {
        ShowSpellSuggestions = false;
    }

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

    // AI Commands
    partial void OnAiApiKeyChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Length > 10)
        {
            _services?.AiService?.Configure(value);
            IsAiConfigured = _services?.AiService?.IsConfigured ?? false;
            if (IsAiConfigured)
                AiStatusMessage = "AI service configured";
        }
        else
        {
            IsAiConfigured = false;
        }
    }

    [RelayCommand]
    private async Task GenerateAiSummaryAsync()
    {
        if (_services?.AiService == null || !_services.AiService.IsConfigured)
        {
            AiStatusMessage = "Please configure AI API key first";
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

            var result = await _services.AiService.GenerateSummaryAsync(
                JobTitle,
                experiences,
                skills);

            if (result.Success && result.Data != null)
            {
                AiGeneratedSummary = result.Data;
                AiStatusMessage = "Summary generated! Click 'Apply' to use it.";
            }
            else
            {
                AiStatusMessage = result.ErrorMessage ?? "Failed to generate summary";
            }
        }
        catch (Exception ex)
        {
            AiStatusMessage = $"Error: {ex.Message}";
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
            AiStatusMessage = "Summary applied!";
            AiGeneratedSummary = null;
        }
    }

    [RelayCommand]
    private async Task SuggestAiSkillsAsync()
    {
        if (_services?.AiService == null || !_services.AiService.IsConfigured)
        {
            AiStatusMessage = "Please configure AI API key first";
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

            var result = await _services.AiService.SuggestSkillsAsync(
                JobTitle,
                currentSkills,
                experiences);

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
            }
        }
        catch (Exception ex)
        {
            AiStatusMessage = $"Error: {ex.Message}";
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

        var skill = new SkillViewModel(new Skill { Name = skillName, Order = Skills.Count }, OnSubViewModelChanged);
        Skills.Add(skill);
        AiSkillSuggestions.Remove(skillName);
        OnSubViewModelChanged();
        StatusMessage = $"Added skill: {skillName}";
    }

    [RelayCommand]
    private void DismissSuggestedSkill(string skillName)
    {
        AiSkillSuggestions.Remove(skillName);
    }

    // Keyword Optimization Commands
    private readonly KeywordAnalyzer _keywordAnalyzer = new();

    [RelayCommand]
    private void AnalyzeKeywords()
    {
        if (string.IsNullOrWhiteSpace(JobDescriptionText))
        {
            StatusMessage = "Please paste a job description first";
            return;
        }

        // Build resume content for analysis
        var resumeContent = BuildResumeTextContent();

        var result = _keywordAnalyzer.Analyze(resumeContent, JobDescriptionText);

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
        StatusMessage = $"Keyword analysis complete: {KeywordMatchScore}% match";
    }

    private string BuildResumeTextContent()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"{FirstName} {LastName}");
        sb.AppendLine(JobTitle);
        sb.AppendLine(Summary);

        foreach (var exp in Experiences)
        {
            sb.AppendLine($"{exp.JobTitle} at {exp.Company}");
            sb.AppendLine(exp.Description);
            sb.AppendLine(exp.Achievements);
        }

        foreach (var edu in EducationList)
        {
            sb.AppendLine($"{edu.Degree} in {edu.FieldOfStudy}");
            sb.AppendLine(edu.Institution);
        }

        foreach (var skill in Skills)
        {
            sb.AppendLine(skill.Name);
            if (!string.IsNullOrEmpty(skill.Category))
                sb.AppendLine(skill.Category);
        }

        foreach (var proj in Projects)
        {
            sb.AppendLine(proj.Name);
            sb.AppendLine(proj.Description);
            sb.AppendLine(proj.Technologies);
        }

        foreach (var cert in Certifications)
        {
            sb.AppendLine(cert.Name);
            sb.AppendLine(cert.IssuingOrganization);
        }

        return sb.ToString();
    }

    [RelayCommand]
    private void AddMissingKeywordAsSkill(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;

        var skill = new SkillViewModel(new Skill { Name = keyword, Order = Skills.Count }, OnSubViewModelChanged);
        Skills.Add(skill);
        MissingKeywords.Remove(keyword);
        OnSubViewModelChanged();
        StatusMessage = $"Added skill: {keyword}";

        // Re-analyze to update scores
        if (HasKeywordAnalysis)
            AnalyzeKeywords();
    }

    [RelayCommand]
    private void ClearKeywordAnalysis()
    {
        JobDescriptionText = "";
        KeywordMatchScore = 0;
        MatchedKeywords.Clear();
        MissingKeywords.Clear();
        KeywordSuggestions.Clear();
        KeywordWarnings.Clear();
        HasKeywordAnalysis = false;
    }

    // Sync Commands
    [RelayCommand]
    private async Task ConfigureSyncAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncFolderPath))
        {
            StatusMessage = "Please specify a sync folder path";
            return;
        }

        if (_services?.SyncService == null)
            return;

        var result = await _services.SyncService.ConfigureAsync(SyncFolderPath);
        IsSyncConfigured = result;

        if (result)
        {
            SyncStatusText = "Configured - Ready to sync";
            StatusMessage = "Sync configured successfully";
            await RefreshRemoteResumesAsync();
        }
        else
        {
            SyncStatusText = "Configuration failed";
            StatusMessage = "Failed to configure sync folder";
        }
    }

    [RelayCommand]
    private async Task BrowseSyncFolderAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

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
            StatusMessage = $"Error selecting folder: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SyncCurrentResumeAsync()
    {
        if (_services?.SyncService == null || !IsSyncConfigured || CurrentResume == null)
        {
            StatusMessage = "Sync not configured or no resume selected";
            return;
        }

        IsSyncing = true;
        SyncStatusText = "Syncing...";

        try
        {
            // Export current resume as JSON
            var json = System.Text.Json.JsonSerializer.Serialize(CurrentResume, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var fileName = $"{SanitizeFileName(CurrentResume.Name ?? "Untitled")}.json";
            var result = await _services.SyncService.UploadAsync(json, fileName);

            if (result)
            {
                SyncStatusText = $"Synced: {DateTime.Now:HH:mm}";
                StatusMessage = "Resume synced successfully";
                await RefreshRemoteResumesAsync();
            }
            else
            {
                SyncStatusText = "Sync failed";
                StatusMessage = "Failed to sync resume";
            }
        }
        catch (Exception ex)
        {
            SyncStatusText = "Error";
            StatusMessage = $"Sync error: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
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
            StatusMessage = $"Error listing remote files: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportRemoteResumeAsync(string remotePath)
    {
        if (_services?.SyncService == null || string.IsNullOrEmpty(remotePath))
            return;

        IsSyncing = true;

        try
        {
            var json = await _services.SyncService.DownloadAsync(remotePath);
            if (json == null)
            {
                StatusMessage = "Failed to download resume";
                return;
            }

            var resume = System.Text.Json.JsonSerializer.Deserialize<Resume>(json);
            if (resume != null)
            {
                CurrentResume = resume;
                LoadResumeIntoEditor(resume);
                UpdatePreviewDebounced();
                StatusMessage = "Resume imported from sync folder";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import error: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    partial void OnSummaryChanged(string value)
    {
        SyncEditorToResume();
        UpdatePreviewDebounced();
        CheckSpellingDebounced();
    }

    private void UpdatePreviewDebounced()
    {
        _previewDebounceTimer?.Stop();
        _previewDebounceTimer?.Dispose();

        _previewDebounceTimer = new System.Timers.Timer(300);
        _previewDebounceTimer.Elapsed += (s, e) =>
        {
            _previewDebounceTimer?.Stop();
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(UpdatePreview);
        };
        _previewDebounceTimer.AutoReset = false;
        _previewDebounceTimer.Start();
    }

    private void UpdatePreview()
    {
        try
        {
            SyncEditorToResume();

            var templateId = SelectedTemplate?.Id ?? "modern";
            var template = _services.TemplateRegistry.GetTemplateOrDefault(templateId);
            var document = template.CreateDocument(CurrentResume);

            var images = document.GenerateImages(new ImageGenerationSettings
            {
                ImageFormat = ImageFormat.Png,
                RasterDpi = 100
            });

            var pageList = new ObservableCollection<Bitmap>();
            foreach (var pageBytes in images)
            {
                using var stream = new MemoryStream(pageBytes);
                pageList.Add(new Bitmap(stream));
            }
            PreviewPages = pageList;

            // Keep first page for backward compatibility
            PreviewImage = pageList.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Preview error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveCurrentResumeAsync()
    {
        try
        {
            SyncEditorToResume();

            if (CurrentResume.Id == 0)
            {
                await _services.Repository.CreateAsync(CurrentResume);
            }
            else
            {
                await _services.Repository.UpdateAsync(CurrentResume);
            }

            await LoadSavedResumesAsync();
            StatusMessage = $"Saved at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadResumeAsync(Resume resume)
    {
        var loaded = await _services.Repository.GetByIdAsync(resume.Id);
        if (loaded != null)
        {
            CurrentResume = loaded;
            LoadResumeIntoEditor(CurrentResume);
            UpdatePreviewDebounced();
            ShowResumeList = false;
            StatusMessage = $"Loaded: {CurrentResume.Name}";
        }
    }

    [RelayCommand]
    private async Task DeleteResumeAsync(Resume resume)
    {
        await _services.Repository.DeleteAsync(resume.Id);
        await LoadSavedResumesAsync();
        StatusMessage = $"Deleted: {resume.Name}";
    }

    [RelayCommand]
    private async Task DuplicateResumeAsync(Resume resume)
    {
        await _services.Repository.DuplicateAsync(resume.Id);
        await LoadSavedResumesAsync();
        StatusMessage = $"Duplicated: {resume.Name}";
    }

    [RelayCommand]
    private void SelectTemplate(TemplateInfo template)
    {
        SelectedTemplate = template;
        CurrentResume.SelectedTemplateId = template.Id;
        ShowTemplateGallery = false;
        UpdatePreviewDebounced();
        StatusMessage = $"Template: {template.Name}";
    }

    [RelayCommand]
    private async Task ExportAsync(string format)
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null)
            {
                StatusMessage = "Cannot open file dialog";
                return;
            }

            SyncEditorToResume();

            var fileName = $"{CurrentResume.PersonalInfo.FullName.Replace(" ", "_")}_Resume";
            var exporter = _services.ExportService.GetExporter(format);

            if (exporter == null)
            {
                StatusMessage = $"Unknown export format: {format}";
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = $"Export as {format}",
                SuggestedFileName = fileName,
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
            {
                // User cancelled
                return;
            }

            IsLoading = true;
            var filePath = file.Path.LocalPath;

            await _services.ExportService.ExportToFileAsync(CurrentResume, format, filePath);
            StatusMessage = $"Exported to: {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportJsonResumeAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Import JSON Resume",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("JSON Resume")
                    {
                        Patterns = new[] { "*.json" },
                        MimeTypes = new[] { "application/json" }
                    }
                }
            });

            if (files.Count == 0) return;

            IsLoading = true;
            StatusMessage = "Importing...";

            await using var stream = await files[0].OpenReadAsync();
            var result = await _services.ExportService.ImportAsync(stream, "JSON Resume");

            if (result.Success && result.Data != null)
            {
                CurrentResume = result.Data;
                LoadResumeIntoEditor(CurrentResume);
                UpdatePreviewDebounced();

                var warningText = result.Warnings.Any()
                    ? $" ({result.Warnings.Count} warnings)"
                    : "";
                StatusMessage = $"Imported successfully{warningText}";
            }
            else
            {
                StatusMessage = $"Import failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportLinkedInAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Import LinkedIn Data Export",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("LinkedIn Export (ZIP)")
                    {
                        Patterns = new[] { "*.zip" },
                        MimeTypes = new[] { "application/zip" }
                    }
                }
            });

            if (files.Count == 0) return;

            IsLoading = true;
            StatusMessage = "Importing LinkedIn data...";

            await using var stream = await files[0].OpenReadAsync();
            var result = await _services.ExportService.ImportAsync(stream, "LinkedIn");

            if (result.Success && result.Data != null)
            {
                CurrentResume = result.Data;
                LoadResumeIntoEditor(CurrentResume);
                UpdatePreviewDebounced();

                var warningText = result.Warnings.Any()
                    ? $" (with {result.Warnings.Count} notes)"
                    : "";
                StatusMessage = $"LinkedIn data imported successfully{warningText}";
            }
            else
            {
                StatusMessage = $"Import failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportPdfAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Import PDF Resume",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("PDF Resume")
                    {
                        Patterns = new[] { "*.pdf" },
                        MimeTypes = new[] { "application/pdf" }
                    }
                }
            });

            if (files.Count == 0) return;

            IsLoading = true;
            StatusMessage = "Importing PDF resume (this may take a moment)...";

            await using var stream = await files[0].OpenReadAsync();
            var result = await _services.ExportService.ImportAsync(stream, "PDF");

            if (result.Success && result.Data != null)
            {
                CurrentResume = result.Data;
                LoadResumeIntoEditor(CurrentResume);
                UpdatePreviewDebounced();

                var warningText = result.Warnings.Any()
                    ? $" ({result.Warnings.Count} sections need review)"
                    : "";
                StatusMessage = $"PDF imported successfully{warningText}. Please review extracted data.";
            }
            else
            {
                StatusMessage = $"Import failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddExperience()
    {
        var exp = new ExperienceViewModel(new Experience { Order = Experiences.Count }, OnSubViewModelChanged);
        var action = CollectionChangeAction<ExperienceViewModel>.Add(
            Experiences, exp, -1, "Experience", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void RemoveExperience(ExperienceViewModel exp)
    {
        var index = Experiences.IndexOf(exp);
        if (index < 0) return;
        var action = CollectionChangeAction<ExperienceViewModel>.Remove(
            Experiences, exp, index, "Experience", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void AddEducation()
    {
        var edu = new EducationViewModel(new Education { Order = EducationList.Count }, OnSubViewModelChanged);
        var action = CollectionChangeAction<EducationViewModel>.Add(
            EducationList, edu, -1, "Education", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void RemoveEducation(EducationViewModel edu)
    {
        var index = EducationList.IndexOf(edu);
        if (index < 0) return;
        var action = CollectionChangeAction<EducationViewModel>.Remove(
            EducationList, edu, index, "Education", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void AddSkill()
    {
        var skill = new SkillViewModel(new Skill { Order = Skills.Count }, OnSubViewModelChanged);
        var action = CollectionChangeAction<SkillViewModel>.Add(
            Skills, skill, -1, "Skill", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void RemoveSkill(SkillViewModel skill)
    {
        var index = Skills.IndexOf(skill);
        if (index < 0) return;
        var action = CollectionChangeAction<SkillViewModel>.Remove(
            Skills, skill, index, "Skill", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void AddLanguage()
    {
        var lang = new LanguageViewModel(new Language { Order = Languages.Count }, OnSubViewModelChanged);
        var action = CollectionChangeAction<LanguageViewModel>.Add(
            Languages, lang, -1, "Language", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void RemoveLanguage(LanguageViewModel lang)
    {
        var index = Languages.IndexOf(lang);
        if (index < 0) return;
        var action = CollectionChangeAction<LanguageViewModel>.Remove(
            Languages, lang, index, "Language", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void AddCertification()
    {
        var cert = new CertificationViewModel(new Certification { Order = Certifications.Count }, OnSubViewModelChanged);
        var action = CollectionChangeAction<CertificationViewModel>.Add(
            Certifications, cert, -1, "Certification", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void RemoveCertification(CertificationViewModel cert)
    {
        var index = Certifications.IndexOf(cert);
        if (index < 0) return;
        var action = CollectionChangeAction<CertificationViewModel>.Remove(
            Certifications, cert, index, "Certification", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void AddProject()
    {
        var proj = new ProjectViewModel(new Project { Order = Projects.Count }, OnSubViewModelChanged);
        var action = CollectionChangeAction<ProjectViewModel>.Add(
            Projects, proj, -1, "Project", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    [RelayCommand]
    private void RemoveProject(ProjectViewModel proj)
    {
        var index = Projects.IndexOf(proj);
        if (index < 0) return;
        var action = CollectionChangeAction<ProjectViewModel>.Remove(
            Projects, proj, index, "Project", OnSubViewModelChanged);
        _services?.UndoRedoManager?.Execute(action);
    }

    // Item reordering commands
    [RelayCommand]
    private void MoveExperienceUp(ExperienceViewModel exp)
    {
        var index = Experiences.IndexOf(exp);
        if (index > 0)
        {
            var action = CollectionChangeAction<ExperienceViewModel>.Move(
                Experiences, index, index - 1, "Experience", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveExperienceDown(ExperienceViewModel exp)
    {
        var index = Experiences.IndexOf(exp);
        if (index < Experiences.Count - 1)
        {
            var action = CollectionChangeAction<ExperienceViewModel>.Move(
                Experiences, index, index + 1, "Experience", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveEducationUp(EducationViewModel edu)
    {
        var index = EducationList.IndexOf(edu);
        if (index > 0)
        {
            var action = CollectionChangeAction<EducationViewModel>.Move(
                EducationList, index, index - 1, "Education", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveEducationDown(EducationViewModel edu)
    {
        var index = EducationList.IndexOf(edu);
        if (index < EducationList.Count - 1)
        {
            var action = CollectionChangeAction<EducationViewModel>.Move(
                EducationList, index, index + 1, "Education", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveSkillUp(SkillViewModel skill)
    {
        var index = Skills.IndexOf(skill);
        if (index > 0)
        {
            var action = CollectionChangeAction<SkillViewModel>.Move(
                Skills, index, index - 1, "Skill", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveSkillDown(SkillViewModel skill)
    {
        var index = Skills.IndexOf(skill);
        if (index < Skills.Count - 1)
        {
            var action = CollectionChangeAction<SkillViewModel>.Move(
                Skills, index, index + 1, "Skill", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveLanguageUp(LanguageViewModel lang)
    {
        var index = Languages.IndexOf(lang);
        if (index > 0)
        {
            var action = CollectionChangeAction<LanguageViewModel>.Move(
                Languages, index, index - 1, "Language", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveLanguageDown(LanguageViewModel lang)
    {
        var index = Languages.IndexOf(lang);
        if (index < Languages.Count - 1)
        {
            var action = CollectionChangeAction<LanguageViewModel>.Move(
                Languages, index, index + 1, "Language", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveCertificationUp(CertificationViewModel cert)
    {
        var index = Certifications.IndexOf(cert);
        if (index > 0)
        {
            var action = CollectionChangeAction<CertificationViewModel>.Move(
                Certifications, index, index - 1, "Certification", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveCertificationDown(CertificationViewModel cert)
    {
        var index = Certifications.IndexOf(cert);
        if (index < Certifications.Count - 1)
        {
            var action = CollectionChangeAction<CertificationViewModel>.Move(
                Certifications, index, index + 1, "Certification", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveProjectUp(ProjectViewModel proj)
    {
        var index = Projects.IndexOf(proj);
        if (index > 0)
        {
            var action = CollectionChangeAction<ProjectViewModel>.Move(
                Projects, index, index - 1, "Project", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void MoveProjectDown(ProjectViewModel proj)
    {
        var index = Projects.IndexOf(proj);
        if (index < Projects.Count - 1)
        {
            var action = CollectionChangeAction<ProjectViewModel>.Move(
                Projects, index, index + 1, "Project", UpdatePreviewDebounced);
            _services?.UndoRedoManager?.Execute(action);
        }
    }

    [RelayCommand]
    private void ZoomIn()
    {
        PreviewZoom = Math.Min(PreviewZoom + 0.1, 2.0);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        PreviewZoom = Math.Max(PreviewZoom - 0.1, 0.5);
    }

    [RelayCommand]
    private void ZoomReset()
    {
        PreviewZoom = 1.0;
    }

    public void TriggerPreviewUpdate()
    {
        UpdatePreviewDebounced();
    }
}

// Sub-ViewModels for collections
public partial class ExperienceViewModel : ObservableObject
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

    private readonly Action? _onChanged;

    public static string[] Months { get; } = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
    public static int[] Years { get; } = Enumerable.Range(1970, DateTime.Now.Year - 1970 + 10).Reverse().ToArray();

    public ExperienceViewModel(Experience exp, Action? onChanged = null)
    {
        _onChanged = onChanged;
        JobTitle = exp.JobTitle;
        Company = exp.Company;
        Location = exp.Location;
        if (exp.StartDate.HasValue)
        {
            StartMonthName = Months[exp.StartDate.Value.Month - 1];
            StartYear = exp.StartDate.Value.Year;
        }
        if (exp.EndDate.HasValue)
        {
            EndMonthName = Months[exp.EndDate.Value.Month - 1];
            EndYear = exp.EndDate.Value.Year;
        }
        IsCurrentRole = exp.IsCurrentRole;
        Description = exp.Description;
        Achievements = string.Join("\n", exp.Achievements);
    }

    partial void OnJobTitleChanged(string value) => _onChanged?.Invoke();
    partial void OnCompanyChanged(string value) => _onChanged?.Invoke();
    partial void OnLocationChanged(string value) => _onChanged?.Invoke();
    partial void OnStartMonthNameChanged(string? value) => _onChanged?.Invoke();
    partial void OnStartYearChanged(int? value) => _onChanged?.Invoke();
    partial void OnEndMonthNameChanged(string? value) => _onChanged?.Invoke();
    partial void OnEndYearChanged(int? value) => _onChanged?.Invoke();
    partial void OnIsCurrentRoleChanged(bool value) => _onChanged?.Invoke();
    partial void OnDescriptionChanged(string value) => _onChanged?.Invoke();
    partial void OnAchievementsChanged(string value) => _onChanged?.Invoke();

    private int? MonthNameToNumber(string? monthName) =>
        monthName != null ? Array.IndexOf(Months, monthName) + 1 : null;

    public Experience ToModel() => new()
    {
        JobTitle = JobTitle,
        Company = Company,
        Location = Location,
        StartDate = MonthNameToNumber(StartMonthName) is int sm && StartYear.HasValue ? new DateTime(StartYear.Value, sm, 1) : null,
        EndDate = MonthNameToNumber(EndMonthName) is int em && EndYear.HasValue ? new DateTime(EndYear.Value, em, 1) : null,
        IsCurrentRole = IsCurrentRole,
        Description = Description,
        Achievements = Achievements.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList()
    };
}

public partial class EducationViewModel : ObservableObject
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

    private readonly Action? _onChanged;

    public static int[] Years { get; } = Enumerable.Range(1970, DateTime.Now.Year - 1970 + 10).Reverse().ToArray();

    public EducationViewModel(Education edu, Action? onChanged = null)
    {
        _onChanged = onChanged;
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

    partial void OnDegreeChanged(string value) => _onChanged?.Invoke();
    partial void OnFieldOfStudyChanged(string value) => _onChanged?.Invoke();
    partial void OnInstitutionChanged(string value) => _onChanged?.Invoke();
    partial void OnLocationChanged(string value) => _onChanged?.Invoke();
    partial void OnStartYearChanged(int? value) => _onChanged?.Invoke();
    partial void OnEndYearChanged(int? value) => _onChanged?.Invoke();
    partial void OnIsCurrentlyStudyingChanged(bool value) => _onChanged?.Invoke();
    partial void OnGradeChanged(string value) => _onChanged?.Invoke();
    partial void OnDescriptionChanged(string value) => _onChanged?.Invoke();

    public Education ToModel() => new()
    {
        Degree = Degree,
        FieldOfStudy = FieldOfStudy,
        Institution = Institution,
        Location = Location,
        StartDate = StartYear.HasValue ? new DateTime(StartYear.Value, 1, 1) : null,
        EndDate = EndYear.HasValue ? new DateTime(EndYear.Value, 1, 1) : null,
        IsCurrentlyStudying = IsCurrentlyStudying,
        Grade = Grade,
        Description = Description
    };
}

public partial class SkillViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _category = "";
    [ObservableProperty] private SkillLevel _level = SkillLevel.Intermediate;

    private readonly Action? _onChanged;

    public SkillViewModel(Skill skill, Action? onChanged = null)
    {
        _onChanged = onChanged;
        Name = skill.Name;
        Category = skill.Category;
        Level = skill.Level;
    }

    partial void OnNameChanged(string value) => _onChanged?.Invoke();
    partial void OnCategoryChanged(string value) => _onChanged?.Invoke();
    partial void OnLevelChanged(SkillLevel value) => _onChanged?.Invoke();

    public Skill ToModel() => new()
    {
        Name = Name,
        Category = Category,
        Level = Level
    };
}

public partial class LanguageViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private LanguageProficiency _proficiency = LanguageProficiency.Professional;

    private readonly Action? _onChanged;

    public static LanguageProficiency[] AvailableProficiencies { get; } = new[]
    {
        LanguageProficiency.Basic,
        LanguageProficiency.Conversational,
        LanguageProficiency.Professional,
        LanguageProficiency.Fluent,
        LanguageProficiency.Native
    };

    public LanguageViewModel(Language lang, Action? onChanged = null)
    {
        _onChanged = onChanged;
        Name = lang.Name;
        Proficiency = lang.Proficiency;
    }

    partial void OnNameChanged(string value) => _onChanged?.Invoke();
    partial void OnProficiencyChanged(LanguageProficiency value) => _onChanged?.Invoke();

    public Language ToModel() => new()
    {
        Name = Name,
        Proficiency = Proficiency
    };
}

public partial class CertificationViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _issuingOrganization = "";
    [ObservableProperty] private DateTimeOffset? _issueDate;

    private readonly Action? _onChanged;

    public CertificationViewModel(Certification cert, Action? onChanged = null)
    {
        _onChanged = onChanged;
        Name = cert.Name;
        IssuingOrganization = cert.IssuingOrganization;
        IssueDate = cert.IssueDate.HasValue ? new DateTimeOffset(cert.IssueDate.Value) : null;
    }

    partial void OnNameChanged(string value) => _onChanged?.Invoke();
    partial void OnIssuingOrganizationChanged(string value) => _onChanged?.Invoke();
    partial void OnIssueDateChanged(DateTimeOffset? value) => _onChanged?.Invoke();

    public Certification ToModel() => new()
    {
        Name = Name,
        IssuingOrganization = IssuingOrganization,
        IssueDate = IssueDate?.DateTime
    };
}

public partial class ProjectViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _url = "";
    [ObservableProperty] private string _technologies = "";

    private readonly Action? _onChanged;

    public ProjectViewModel(Project proj, Action? onChanged = null)
    {
        _onChanged = onChanged;
        Name = proj.Name;
        Description = proj.Description;
        Url = proj.Url;
        Technologies = string.Join(", ", proj.Technologies);
    }

    partial void OnNameChanged(string value) => _onChanged?.Invoke();
    partial void OnDescriptionChanged(string value) => _onChanged?.Invoke();
    partial void OnUrlChanged(string value) => _onChanged?.Invoke();
    partial void OnTechnologiesChanged(string value) => _onChanged?.Invoke();

    public Project ToModel() => new()
    {
        Name = Name,
        Description = Description,
        Url = Url,
        Technologies = Technologies.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim()).ToList()
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

    partial void OnIsVisibleChanged(bool value)
    {
        _onChanged?.Invoke();
    }
}

public partial class SpellSuggestionViewModel : ObservableObject
{
    [ObservableProperty] private string _misspelledWord = "";
    [ObservableProperty] private ObservableCollection<string> _suggestions = new();

    private readonly Action<string> _onApply;
    private readonly Action<string> _onAddToDictionary;

    public SpellSuggestionViewModel(string word, IEnumerable<string> suggestions,
        Action<string> onApply, Action<string> onAddToDictionary)
    {
        MisspelledWord = word;
        Suggestions = new ObservableCollection<string>(suggestions);
        _onApply = onApply;
        _onAddToDictionary = onAddToDictionary;
    }

    [RelayCommand]
    private void ApplySuggestion(string suggestion)
    {
        _onApply(suggestion);
    }

    [RelayCommand]
    private void AddToDictionary()
    {
        _onAddToDictionary(MisspelledWord);
    }
}

public class KeywordMatchViewModel
{
    public string Keyword { get; init; } = "";
    public string Category { get; init; } = "";
    public int JobCount { get; init; }
    public int ResumeCount { get; init; }
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
