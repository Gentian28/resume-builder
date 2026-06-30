using System.Globalization;
using System.IO.Compression;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export.Models;

namespace ResumeBuilder.Export.Importers;

public class LinkedInImporter : IImporter
{
    public string Format => "LinkedIn";
    public string[] SupportedExtensions => new[] { ".zip" };

    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        MissingFieldFound = null,
        HeaderValidated = null,
        BadDataFound = null,
        IgnoreBlankLines = true
    };

    public async Task<ImportResult<Resume>> ImportAsync(Stream stream)
    {
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var warnings = new List<string>();
            var resume = new Resume();

            // Parse Profile
            var profile = await ReadCsvFromArchive<LinkedInProfile>(archive, "Profile.csv");
            if (profile.Any())
            {
                var p = profile.First();
                resume.PersonalInfo.FirstName = p.FirstName ?? "";
                resume.PersonalInfo.LastName = p.LastName ?? "";
                resume.PersonalInfo.JobTitle = p.Headline ?? "";
                resume.Summary = p.Summary ?? "";
                resume.PersonalInfo.Address = p.Address ?? "";
                resume.PersonalInfo.PostalCode = p.ZipCode ?? "";

                // Parse location
                if (!string.IsNullOrEmpty(p.GeoLocation))
                {
                    var locationParts = p.GeoLocation.Split(',');
                    if (locationParts.Length >= 1)
                        resume.PersonalInfo.City = locationParts[0].Trim();
                    if (locationParts.Length >= 2)
                        resume.PersonalInfo.Country = locationParts[^1].Trim();
                }

                // Parse websites
                if (!string.IsNullOrEmpty(p.Websites))
                {
                    var websites = p.Websites.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var site in websites)
                    {
                        var url = site.Trim();
                        if (url.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase))
                            resume.PersonalInfo.LinkedIn = url;
                        else if (url.Contains("github.com", StringComparison.OrdinalIgnoreCase))
                            resume.PersonalInfo.GitHub = url;
                        else if (string.IsNullOrEmpty(resume.PersonalInfo.Website))
                            resume.PersonalInfo.Website = url;
                    }
                }
            }
            else
            {
                warnings.Add("Profile.csv not found or empty. Personal information will be incomplete.");
            }

            // Parse Email addresses
            var emails = await ReadCsvFromArchive<LinkedInEmail>(archive, "Email Addresses.csv");
            var primaryEmail = emails.FirstOrDefault(e => e.Primary?.ToLower() == "yes") ?? emails.FirstOrDefault();
            if (primaryEmail != null)
            {
                resume.PersonalInfo.Email = primaryEmail.EmailAddress ?? "";
            }

            // Parse Phone numbers
            var phones = await ReadCsvFromArchive<LinkedInPhoneNumber>(archive, "PhoneNumbers.csv");
            if (!phones.Any())
                phones = await ReadCsvFromArchive<LinkedInPhoneNumber>(archive, "Phone Numbers.csv");
            var primaryPhone = phones.FirstOrDefault();
            if (primaryPhone != null)
            {
                resume.PersonalInfo.Phone = primaryPhone.Number ?? "";
            }

            // Parse Positions (Work Experience)
            var positions = await ReadCsvFromArchive<LinkedInPosition>(archive, "Positions.csv");
            resume.Experiences = positions.Select((p, i) => new Experience
            {
                Order = i,
                JobTitle = p.Title ?? "",
                Company = p.CompanyName ?? "",
                Location = p.Location ?? "",
                Description = p.Description ?? "",
                StartDate = ParseLinkedInDate(p.StartedOn),
                EndDate = ParseLinkedInDate(p.FinishedOn),
                IsCurrentRole = string.IsNullOrWhiteSpace(p.FinishedOn)
            }).ToList();

            if (!positions.Any())
                warnings.Add("Positions.csv not found. Work experience will be empty.");

            // Parse Education
            var education = await ReadCsvFromArchive<LinkedInEducation>(archive, "Education.csv");
            resume.EducationList = education.Select((e, i) => new Education
            {
                Order = i,
                Institution = e.SchoolName ?? "",
                Degree = e.DegreeName ?? "",
                Description = e.Notes ?? "",
                StartDate = ParseLinkedInDate(e.StartDate),
                EndDate = ParseLinkedInDate(e.EndDate),
                IsCurrentlyStudying = string.IsNullOrWhiteSpace(e.EndDate)
            }).ToList();

            if (!education.Any())
                warnings.Add("Education.csv not found. Education will be empty.");

            // Parse Skills
            var skills = await ReadCsvFromArchive<LinkedInSkill>(archive, "Skills.csv");
            resume.Skills = skills.Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select((s, i) => new Skill
                {
                    Order = i,
                    Name = s.Name ?? "",
                    Level = SkillLevel.Intermediate
                }).ToList();

            // Parse Certifications
            var certifications = await ReadCsvFromArchive<LinkedInCertification>(archive, "Certifications.csv");
            resume.Certifications = certifications.Select((c, i) => new Certification
            {
                Order = i,
                Name = c.Name ?? "",
                IssuingOrganization = c.Authority ?? "",
                IssueDate = ParseLinkedInDate(c.StartedOn),
                ExpirationDate = ParseLinkedInDate(c.FinishedOn),
                CredentialId = c.LicenseNumber ?? "",
                CredentialUrl = c.Url ?? ""
            }).ToList();

            // Parse Languages
            var languages = await ReadCsvFromArchive<LinkedInLanguage>(archive, "Languages.csv");
            resume.Languages = languages.Where(l => !string.IsNullOrWhiteSpace(l.Name))
                .Select((l, i) => new Language
                {
                    Order = i,
                    Name = l.Name ?? "",
                    Proficiency = ParseLanguageProficiency(l.Proficiency)
                }).ToList();

            // Parse Projects
            var projects = await ReadCsvFromArchive<LinkedInProject>(archive, "Projects.csv");
            resume.Projects = projects.Select((p, i) => new Project
            {
                Order = i,
                Name = p.Title ?? "",
                Description = p.Description ?? "",
                Url = p.Url ?? "",
                StartDate = ParseLinkedInDate(p.StartedOn),
                EndDate = ParseLinkedInDate(p.FinishedOn),
                IsOngoing = string.IsNullOrWhiteSpace(p.FinishedOn)
            }).ToList();

            // Set resume name
            resume.Name = $"{resume.PersonalInfo.FullName}'s Resume (LinkedIn Import)".Trim();
            if (resume.Name.StartsWith("'s"))
                resume.Name = "LinkedIn Import";

            return ImportResult<Resume>.Succeeded(resume, warnings);
        }
        catch (InvalidDataException)
        {
            return ImportResult<Resume>.Failed("Invalid ZIP file. Please ensure you're uploading a LinkedIn data export ZIP file.");
        }
        catch (Exception ex)
        {
            return ImportResult<Resume>.Failed($"Error importing LinkedIn data: {ex.Message}");
        }
    }

    public async Task<ImportResult<Resume>> ImportFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return ImportResult<Resume>.Failed($"File not found: {filePath}");

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
            return ImportResult<Resume>.Failed($"Unsupported file type. Expected ZIP file.");

        await using var stream = File.OpenRead(filePath);
        return await ImportAsync(stream);
    }

    private async Task<List<T>> ReadCsvFromArchive<T>(ZipArchive archive, string fileName)
    {
        // Try different file name variations
        var fileNames = new[]
        {
            fileName,
            fileName.Replace(" ", ""),
            fileName.Replace(".csv", "").Replace(" ", "") + ".csv"
        };

        ZipArchiveEntry? entry = null;
        foreach (var name in fileNames)
        {
            entry = archive.Entries.FirstOrDefault(e =>
                e.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                e.FullName.EndsWith(name, StringComparison.OrdinalIgnoreCase));
            if (entry != null) break;
        }

        if (entry == null)
            return new List<T>();

        try
        {
            await using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream, Encoding.UTF8);
            using var csv = new CsvReader(reader, CsvConfig);
            return csv.GetRecords<T>().ToList();
        }
        catch
        {
            return new List<T>();
        }
    }

    private static DateTime? ParseLinkedInDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        // LinkedIn uses various date formats: "Jan 2020", "2020", "01/2020", etc.
        var formats = new[]
        {
            "MMM yyyy",
            "MMMM yyyy",
            "MM/yyyy",
            "yyyy-MM-dd",
            "yyyy-MM",
            "yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateStr.Trim(), format, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        // Try general parse as fallback
        if (DateTime.TryParse(dateStr, out var generalDate))
            return generalDate;

        return null;
    }

    private static LanguageProficiency ParseLanguageProficiency(string? proficiency)
    {
        if (string.IsNullOrWhiteSpace(proficiency))
            return LanguageProficiency.Professional;

        return proficiency.ToLowerInvariant() switch
        {
            "elementary" or "elementary proficiency" => LanguageProficiency.Basic,
            "limited working" or "limited working proficiency" => LanguageProficiency.Conversational,
            "professional working" or "professional working proficiency" => LanguageProficiency.Professional,
            "full professional" or "full professional proficiency" => LanguageProficiency.Fluent,
            "native" or "native or bilingual" or "native or bilingual proficiency" => LanguageProficiency.Native,
            _ => LanguageProficiency.Professional
        };
    }
}
