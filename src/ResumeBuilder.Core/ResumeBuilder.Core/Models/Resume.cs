using System.Text.Json.Serialization;

namespace ResumeBuilder.Core.Models;

public class Resume
{
    public int Id { get; set; }
    public string Name { get; set; } = "Untitled Resume";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string SelectedTemplateId { get; set; } = "modern";

    // Template customization (legacy - kept for compatibility)
    public string AccentColor { get; set; } = "#2563eb";
    public string FontFamily { get; set; } = "Arial";

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
            var start = StartDate?.ToString("MMM yyyy") ?? "";
            var end = IsCurrentRole ? "Present" : (EndDate?.ToString("MMM yyyy") ?? "");
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
            var start = StartDate?.ToString("yyyy") ?? "";
            var end = IsCurrentlyStudying ? "Present" : (EndDate?.ToString("yyyy") ?? "");
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
