using System.Text.Json.Serialization;

namespace ResumeBuilder.Core.Models;

/// <summary>
/// A cover letter for one application. It borrows the sender block and styling from a resume so the
/// two documents look like a matched pair rather than two unrelated files.
/// </summary>
public class CoverLetter
{
    public int Id { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public Guid RowVersion { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Untitled Cover Letter";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The resume this letter accompanies; its PersonalInfo and styling are reused.</summary>
    public int? ResumeId { get; set; }

    public string SelectedTemplateId { get; set; } = "letter-modern";
    public TemplateSettings TemplateSettings { get; set; } = new();

    /// <summary>Sender block. Copied from the resume on creation, then editable independently.</summary>
    public PersonalInfo PersonalInfo { get; set; } = new();

    // Recipient
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;

    public DateTime LetterDate { get; set; } = DateTime.Today;
    public string Subject { get; set; } = string.Empty;

    /// <summary>Defaults to "Dear Hiring Manager," when no recipient is known.</summary>
    public string Salutation { get; set; } = string.Empty;

    /// <summary>The letter itself, one string per paragraph.</summary>
    public List<string> Paragraphs { get; set; } = new();

    public string Closing { get; set; } = "Sincerely,";

    /// <summary>The posting this letter was written against, kept for re-tailoring.</summary>
    public string JobDescription { get; set; } = string.Empty;

    [JsonIgnore]
    public string EffectiveSalutation => string.IsNullOrWhiteSpace(Salutation)
        ? (string.IsNullOrWhiteSpace(RecipientName) ? "Dear Hiring Manager," : $"Dear {RecipientName},")
        : Salutation;

    [JsonIgnore]
    public string SignatureName => PersonalInfo.FullName;

    [JsonIgnore]
    public string Body => string.Join(Environment.NewLine + Environment.NewLine, Paragraphs);

    /// <summary>
    /// Starts a letter from a resume, inheriting the sender block and template styling so the pair
    /// looks consistent.
    /// </summary>
    public static CoverLetter FromResume(Resume resume, string? companyName = null, string? targetRole = null)
    {
        var role = string.IsNullOrWhiteSpace(targetRole) ? resume.PersonalInfo.JobTitle : targetRole!;

        return new CoverLetter
        {
            ResumeId = resume.Id == 0 ? null : resume.Id,
            Name = string.IsNullOrWhiteSpace(companyName)
                ? "Cover Letter"
                : $"Cover Letter - {companyName}",
            PersonalInfo = ClonePersonalInfo(resume.PersonalInfo),
            TemplateSettings = resume.TemplateSettings.Clone(),
            CompanyName = companyName ?? string.Empty,
            Subject = string.IsNullOrWhiteSpace(role) ? string.Empty : $"Application for {role}",
            JobDescription = resume.JobDescription
        };
    }

    private static PersonalInfo ClonePersonalInfo(PersonalInfo source) => new()
    {
        FirstName = source.FirstName,
        LastName = source.LastName,
        JobTitle = source.JobTitle,
        Email = source.Email,
        Phone = source.Phone,
        Address = source.Address,
        City = source.City,
        Country = source.Country,
        PostalCode = source.PostalCode,
        Website = source.Website,
        LinkedIn = source.LinkedIn,
        GitHub = source.GitHub,
        Photo = source.Photo?.ToArray()
    };
}
