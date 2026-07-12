using System.Globalization;
using System.Text.Json.Serialization;

namespace ResumeBuilder.Core.Models;

public class Resume
{
    public int Id { get; set; }
    public string Name { get; set; } = "Untitled Resume";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string SelectedTemplateId { get; set; } = "modern";

    /// <summary>Optimistic-concurrency token; changed on every persisted update.</summary>
    public Guid RowVersion { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Stable identity that survives export/import and differs from <see cref="Id"/>, which is
    /// local to one database. Sync uses this to match the same resume across machines.
    /// </summary>
    public Guid SyncId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Set when this resume is a variant tailored for one application; points at the resume it was
    /// branched from. Null for a base resume.
    /// </summary>
    public int? BaseResumeId { get; set; }

    /// <summary>The role this variant targets, e.g. "Senior Backend Engineer at Northwind".</summary>
    public string TargetRole { get; set; } = string.Empty;

    /// <summary>The job posting this variant was tailored against, kept so it can be re-analyzed.</summary>
    public string JobDescription { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsVariant => BaseResumeId.HasValue;

    /// <summary>
    /// Accent color. Kept as the single source of truth alongside
    /// <see cref="TemplateSettings"/>, which mirrors it via <see cref="SyncLegacyStyling"/>.
    /// </summary>
    public string AccentColor { get; set; } = TemplateSettings.DefaultAccentColor;
    public string FontFamily { get; set; } = TemplateSettings.DefaultFontFamily;

    // Extended template settings
    public TemplateSettings TemplateSettings { get; set; } = new();

    // Section ordering
    public SectionOrder SectionOrder { get; set; } = new();

    // Resume sections
    public PersonalInfo PersonalInfo { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public List<Experience> Experiences { get; set; } = new();
    public List<Education> EducationList { get; set; } = new();
    public List<Skill> Skills { get; set; } = new();
    public List<Language> Languages { get; set; } = new();
    public List<Certification> Certifications { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
    public List<CustomSection> CustomSections { get; set; } = new();

    /// <summary>
    /// Keeps the legacy <see cref="AccentColor"/>/<see cref="FontFamily"/> fields in step with
    /// <see cref="TemplateSettings"/> so exporters reading either one agree.
    /// </summary>
    public void SyncLegacyStyling()
    {
        AccentColor = TemplateSettings.AccentColor;
        FontFamily = TemplateSettings.FontFamily;
    }
}

/// <summary>Culture-invariant date formatting so resume output is identical on every machine.</summary>
public static class ResumeDateFormat
{
    public static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string MonthYear(DateTime? date) => date?.ToString("MMM yyyy", Culture) ?? "";

    public static string Year(DateTime? date) => date?.ToString("yyyy", Culture) ?? "";

    public static string MonthYearOrEmpty(DateTime date) => date.ToString("MMM yyyy", Culture);
}

public class PersonalInfo
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string LinkedIn { get; set; } = string.Empty;
    public string GitHub { get; set; } = string.Empty;
    public byte[]? Photo { get; set; }

    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [JsonIgnore]
    public string Location => string.Join(", ", new[] { City, Country }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public class Experience
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrentRole { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Achievements { get; set; } = new();

    [JsonIgnore]
    public string DateRange
    {
        get
        {
            var start = ResumeDateFormat.MonthYear(StartDate);
            var end = IsCurrentRole ? "Present" : ResumeDateFormat.MonthYear(EndDate);
            return string.IsNullOrEmpty(start) ? "" : $"{start} - {end}";
        }
    }
}

public class Education
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Degree { get; set; } = string.Empty;
    public string FieldOfStudy { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrentlyStudying { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public string DateRange
    {
        get
        {
            var start = ResumeDateFormat.Year(StartDate);
            var end = IsCurrentlyStudying ? "Present" : ResumeDateFormat.Year(EndDate);
            return string.IsNullOrEmpty(start) ? "" : $"{start} - {end}";
        }
    }

    [JsonIgnore]
    public string DegreeWithField => string.IsNullOrEmpty(FieldOfStudy)
        ? Degree
        : $"{Degree} in {FieldOfStudy}";
}

public class Skill
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public SkillLevel Level { get; set; } = SkillLevel.Intermediate;
    public string Category { get; set; } = string.Empty;
}

public enum SkillLevel
{
    Beginner = 1,
    Elementary = 2,
    Intermediate = 3,
    Advanced = 4,
    Expert = 5
}

public class Language
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public LanguageProficiency Proficiency { get; set; } = LanguageProficiency.Professional;
}

public enum LanguageProficiency
{
    Basic = 1,
    Conversational = 2,
    Professional = 3,
    Fluent = 4,
    Native = 5
}

public class Certification
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IssuingOrganization { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool DoesNotExpire { get; set; }
    public string CredentialId { get; set; } = string.Empty;
    public string CredentialUrl { get; set; } = string.Empty;
}

public class Project
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsOngoing { get; set; }
    public List<string> Technologies { get; set; } = new();
    public List<string> Highlights { get; set; } = new();
}

public class CustomSection
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<CustomSectionItem> Items { get; set; } = new();
}

public class CustomSectionItem
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
