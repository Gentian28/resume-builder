using ResumeBuilder.Core.Models;
using ResumeBuilder.Export.Models;

namespace ResumeBuilder.Export.Mappers;

public static class JsonResumeMapper
{
    public static JsonResumeSchema ToJsonResume(Resume resume)
    {
        var nameParts = ParseName(resume.PersonalInfo.FullName);

        return new JsonResumeSchema
        {
            Basics = new JsonResumeBasics
            {
                Name = resume.PersonalInfo.FullName,
                Label = resume.PersonalInfo.JobTitle,
                Email = resume.PersonalInfo.Email,
                Phone = resume.PersonalInfo.Phone,
                Url = resume.PersonalInfo.Website,
                Summary = resume.Summary,
                Location = new JsonResumeLocation
                {
                    Address = resume.PersonalInfo.Address,
                    City = resume.PersonalInfo.City,
                    PostalCode = resume.PersonalInfo.PostalCode,
                    CountryCode = resume.PersonalInfo.Country
                },
                Profiles = GetProfiles(resume.PersonalInfo)
            },
            Work = resume.Experiences.OrderBy(e => e.Order).Select(e => new JsonResumeWork
            {
                Name = e.Company,
                Position = e.JobTitle,
                Location = e.Location,
                StartDate = e.StartDate?.ToString("yyyy-MM-dd"),
                EndDate = e.IsCurrentRole ? null : e.EndDate?.ToString("yyyy-MM-dd"),
                Summary = e.Description,
                Highlights = e.Achievements.Any() ? e.Achievements : null
            }).ToList(),
            Education = resume.EducationList.OrderBy(e => e.Order).Select(e => new JsonResumeEducation
            {
                Institution = e.Institution,
                StudyType = e.Degree,
                Area = e.FieldOfStudy,
                StartDate = e.StartDate?.ToString("yyyy-MM-dd"),
                EndDate = e.IsCurrentlyStudying ? null : e.EndDate?.ToString("yyyy-MM-dd"),
                Score = e.Grade
            }).ToList(),
            Skills = GroupSkillsByCategory(resume.Skills),
            Languages = resume.Languages.OrderBy(l => l.Order).Select(l => new JsonResumeLanguage
            {
                Language = l.Name,
                Fluency = MapProficiencyToJsonResume(l.Proficiency)
            }).ToList(),
            Certificates = resume.Certifications.OrderBy(c => c.Order).Select(c => new JsonResumeCertificate
            {
                Name = c.Name,
                Issuer = c.IssuingOrganization,
                Date = c.IssueDate?.ToString("yyyy-MM-dd"),
                Url = c.CredentialUrl
            }).ToList(),
            Projects = resume.Projects.OrderBy(p => p.Order).Select(p => new JsonResumeProject
            {
                Name = p.Name,
                Description = p.Description,
                Url = p.Url,
                StartDate = p.StartDate?.ToString("yyyy-MM-dd"),
                EndDate = p.IsOngoing ? null : p.EndDate?.ToString("yyyy-MM-dd"),
                Keywords = p.Technologies.Any() ? p.Technologies : null,
                Highlights = p.Highlights.Any() ? p.Highlights : null
            }).ToList(),
            Meta = new JsonResumeMeta
            {
                Version = "v1.0.0",
                LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        };
    }

    public static Resume FromJsonResume(JsonResumeSchema jsonResume)
    {
        var nameParts = ParseName(jsonResume.Basics?.Name ?? "");

        var resume = new Resume
        {
            Name = $"{nameParts.firstName} {nameParts.lastName}'s Resume".Trim(),
            Summary = jsonResume.Basics?.Summary ?? "",
            PersonalInfo = new PersonalInfo
            {
                FirstName = nameParts.firstName,
                LastName = nameParts.lastName,
                JobTitle = jsonResume.Basics?.Label ?? "",
                Email = jsonResume.Basics?.Email ?? "",
                Phone = jsonResume.Basics?.Phone ?? "",
                Website = jsonResume.Basics?.Url ?? "",
                Address = jsonResume.Basics?.Location?.Address ?? "",
                City = jsonResume.Basics?.Location?.City ?? "",
                PostalCode = jsonResume.Basics?.Location?.PostalCode ?? "",
                Country = jsonResume.Basics?.Location?.CountryCode ?? ""
            }
        };

        // Parse profiles for LinkedIn and GitHub
        if (jsonResume.Basics?.Profiles != null)
        {
            foreach (var profile in jsonResume.Basics.Profiles)
            {
                if (profile.Network?.ToLowerInvariant() == "linkedin")
                    resume.PersonalInfo.LinkedIn = profile.Url ?? "";
                else if (profile.Network?.ToLowerInvariant() == "github")
                    resume.PersonalInfo.GitHub = profile.Username ?? profile.Url ?? "";
            }
        }

        // Work experience
        if (jsonResume.Work != null)
        {
            resume.Experiences = jsonResume.Work.Select((w, i) => new Experience
            {
                Order = i,
                JobTitle = w.Position ?? "",
                Company = w.Name ?? "",
                Location = w.Location ?? "",
                StartDate = ParseDate(w.StartDate),
                EndDate = ParseDate(w.EndDate),
                IsCurrentRole = string.IsNullOrEmpty(w.EndDate),
                Description = w.Summary ?? "",
                Achievements = w.Highlights ?? new List<string>()
            }).ToList();
        }

        // Education
        if (jsonResume.Education != null)
        {
            resume.EducationList = jsonResume.Education.Select((e, i) => new Education
            {
                Order = i,
                Institution = e.Institution ?? "",
                Degree = e.StudyType ?? "",
                FieldOfStudy = e.Area ?? "",
                StartDate = ParseDate(e.StartDate),
                EndDate = ParseDate(e.EndDate),
                IsCurrentlyStudying = string.IsNullOrEmpty(e.EndDate),
                Grade = e.Score ?? ""
            }).ToList();
        }

        // Skills - flatten grouped skills
        if (jsonResume.Skills != null)
        {
            var skillIndex = 0;
            foreach (var skillGroup in jsonResume.Skills)
            {
                if (skillGroup.Keywords != null)
                {
                    foreach (var keyword in skillGroup.Keywords)
                    {
                        resume.Skills.Add(new Skill
                        {
                            Order = skillIndex++,
                            Name = keyword,
                            Category = skillGroup.Name ?? "",
                            Level = MapJsonResumeLevel(skillGroup.Level)
                        });
                    }
                }
                else
                {
                    resume.Skills.Add(new Skill
                    {
                        Order = skillIndex++,
                        Name = skillGroup.Name ?? "",
                        Level = MapJsonResumeLevel(skillGroup.Level)
                    });
                }
            }
        }

        // Languages
        if (jsonResume.Languages != null)
        {
            resume.Languages = jsonResume.Languages.Select((l, i) => new Language
            {
                Order = i,
                Name = l.Language ?? "",
                Proficiency = MapJsonResumeFluency(l.Fluency)
            }).ToList();
        }

        // Certifications
        if (jsonResume.Certificates != null)
        {
            resume.Certifications = jsonResume.Certificates.Select((c, i) => new Certification
            {
                Order = i,
                Name = c.Name ?? "",
                IssuingOrganization = c.Issuer ?? "",
                IssueDate = ParseDate(c.Date),
                CredentialUrl = c.Url ?? ""
            }).ToList();
        }

        // Projects
        if (jsonResume.Projects != null)
        {
            resume.Projects = jsonResume.Projects.Select((p, i) => new Project
            {
                Order = i,
                Name = p.Name ?? "",
                Description = p.Description ?? "",
                Url = p.Url ?? "",
                StartDate = ParseDate(p.StartDate),
                EndDate = ParseDate(p.EndDate),
                IsOngoing = string.IsNullOrEmpty(p.EndDate),
                Technologies = p.Keywords ?? new List<string>(),
                Highlights = p.Highlights ?? new List<string>()
            }).ToList();
        }

        return resume;
    }

    private static (string firstName, string lastName) ParseName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return ("", "");

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return ("", "");
        if (parts.Length == 1)
            return (parts[0], "");

        return (parts[0], string.Join(" ", parts.Skip(1)));
    }

    private static List<JsonResumeProfile>? GetProfiles(PersonalInfo info)
    {
        var profiles = new List<JsonResumeProfile>();

        if (!string.IsNullOrWhiteSpace(info.LinkedIn))
        {
            profiles.Add(new JsonResumeProfile
            {
                Network = "LinkedIn",
                Url = info.LinkedIn
            });
        }

        if (!string.IsNullOrWhiteSpace(info.GitHub))
        {
            profiles.Add(new JsonResumeProfile
            {
                Network = "GitHub",
                Username = info.GitHub,
                Url = info.GitHub.StartsWith("http") ? info.GitHub : $"https://github.com/{info.GitHub}"
            });
        }

        return profiles.Any() ? profiles : null;
    }

    private static List<JsonResumeSkill> GroupSkillsByCategory(List<Skill> skills)
    {
        var grouped = skills.GroupBy(s => s.Category);
        var result = new List<JsonResumeSkill>();

        foreach (var group in grouped)
        {
            if (string.IsNullOrWhiteSpace(group.Key))
            {
                // No category - add as individual skills
                foreach (var skill in group)
                {
                    result.Add(new JsonResumeSkill
                    {
                        Name = skill.Name,
                        Level = MapSkillLevelToJsonResume(skill.Level)
                    });
                }
            }
            else
            {
                // Group skills by category
                result.Add(new JsonResumeSkill
                {
                    Name = group.Key,
                    Level = MapSkillLevelToJsonResume(group.Max(s => s.Level)),
                    Keywords = group.Select(s => s.Name).ToList()
                });
            }
        }

        return result;
    }

    private static string MapSkillLevelToJsonResume(SkillLevel level) => level switch
    {
        SkillLevel.Beginner => "Beginner",
        SkillLevel.Elementary => "Beginner",
        SkillLevel.Intermediate => "Intermediate",
        SkillLevel.Advanced => "Advanced",
        SkillLevel.Expert => "Master",
        _ => "Intermediate"
    };

    private static SkillLevel MapJsonResumeLevel(string? level) => level?.ToLowerInvariant() switch
    {
        "beginner" => SkillLevel.Beginner,
        "intermediate" => SkillLevel.Intermediate,
        "advanced" => SkillLevel.Advanced,
        "master" or "expert" => SkillLevel.Expert,
        _ => SkillLevel.Intermediate
    };

    private static string MapProficiencyToJsonResume(LanguageProficiency proficiency) => proficiency switch
    {
        LanguageProficiency.Basic => "Elementary",
        LanguageProficiency.Conversational => "Limited Working",
        LanguageProficiency.Professional => "Professional Working",
        LanguageProficiency.Fluent => "Full Professional",
        LanguageProficiency.Native => "Native Speaker",
        _ => "Professional Working"
    };

    private static LanguageProficiency MapJsonResumeFluency(string? fluency) => fluency?.ToLowerInvariant() switch
    {
        "elementary" or "basic" => LanguageProficiency.Basic,
        "limited working" or "conversational" => LanguageProficiency.Conversational,
        "professional working" or "professional" => LanguageProficiency.Professional,
        "full professional" or "fluent" => LanguageProficiency.Fluent,
        "native speaker" or "native" or "bilingual" => LanguageProficiency.Native,
        _ => LanguageProficiency.Professional
    };

    private static DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, out var date))
            return date;

        return null;
    }
}
