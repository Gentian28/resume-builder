using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export;

/// <summary>
/// The résumé every template thumbnail is rendered from.
///
/// Built in code rather than read from samples/sample-resume.json so a thumbnail can never fail
/// because a file was missing or moved at runtime. It mirrors that fixture: entirely synthetic,
/// no real person's details.
///
/// Deliberately full enough to exercise every section a template might render — a thumbnail of an
/// empty résumé shows nothing about the design, which is the whole point of having one.
/// </summary>
public static class ThumbnailSample
{
    /// <summary>
    /// Fresh instance per call: templates clone settings but callers should never be able to
    /// mutate a shared sample and change every subsequent thumbnail.
    /// </summary>
    public static Resume Create() => new()
    {
        Name = "Sample",

        // AccentColor and FontFamily are left at their defaults and IsAccentColorCustomized /
        // IsFontCustomized are not set, so ApplyTemplateDefaults gives each template its own
        // colour and typeface. The card advertises the template, not anyone's document.

        PersonalInfo = new PersonalInfo
        {
            FirstName = "Jane",
            LastName = "Doe",
            JobTitle = "Software Engineer",
            Email = "jane.doe@example.com",
            Phone = "+1 555 0100",
            City = "Springfield",
            Country = "US",
            Website = "https://example.com",
            LinkedIn = "https://www.linkedin.com/in/jane-doe/",
            GitHub = "janedoe"
        },

        Summary =
            "Software engineer with 8 years of experience building web and data platforms across " +
            "the full stack. Leads small teams, designs service architectures, and ships products " +
            "in fintech and SaaS.",

        Experiences =
        [
            new Experience
            {
                Order = 0,
                JobTitle = "Senior Software Engineer",
                Company = "Northwind Systems",
                Location = "Springfield, IL",
                StartDate = new DateTime(2022, 3, 1),
                IsCurrentRole = true,
                Description = "Data platform team, ingesting and normalizing feeds from 150+ partners daily.",
                Achievements =
                [
                    "Rewrote the ingestion pipeline, cutting end-to-end processing from 18 hours to under 5 minutes",
                    "Introduced schema validation that caught malformed partner feeds before they reached production",
                    "Mentored three engineers through their first year on the team"
                ]
            },
            new Experience
            {
                Order = 1,
                JobTitle = "Software Engineer",
                Company = "Contoso Digital",
                Location = "Chicago, IL",
                StartDate = new DateTime(2019, 1, 1),
                EndDate = new DateTime(2022, 2, 1),
                Description = "Payments and integrations for a consumer marketplace.",
                Achievements =
                [
                    "Split a payments monolith into services, removing deploy-time downtime",
                    "Built a shared caching library adopted by four teams"
                ]
            }
        ],

        EducationList =
        [
            new Education
            {
                Order = 0,
                Degree = "Bachelor of Science",
                FieldOfStudy = "Computer Science",
                Institution = "State University",
                StartDate = new DateTime(2013, 9, 1),
                EndDate = new DateTime(2017, 6, 1)
            }
        ],

        Skills =
        [
            new Skill { Order = 0, Name = "C#", Category = "Backend", Level = SkillLevel.Expert },
            new Skill { Order = 1, Name = ".NET", Category = "Backend", Level = SkillLevel.Expert },
            new Skill { Order = 2, Name = "ASP.NET Core", Category = "Backend", Level = SkillLevel.Advanced },
            new Skill { Order = 3, Name = "AWS", Category = "Cloud & Data", Level = SkillLevel.Advanced },
            new Skill { Order = 4, Name = "PostgreSQL", Category = "Cloud & Data", Level = SkillLevel.Advanced },
            new Skill { Order = 5, Name = "TypeScript", Category = "Frontend", Level = SkillLevel.Intermediate },
            new Skill { Order = 6, Name = "React", Category = "Frontend", Level = SkillLevel.Intermediate }
        ],

        Languages =
        [
            new Language { Order = 0, Name = "English", Proficiency = LanguageProficiency.Native },
            new Language { Order = 1, Name = "Spanish", Proficiency = LanguageProficiency.Professional }
        ],

        Certifications =
        [
            new Certification
            {
                Order = 0,
                Name = "AWS Solutions Architect",
                IssuingOrganization = "Amazon Web Services",
                IssueDate = new DateTime(2023, 5, 1),
                DoesNotExpire = true
            }
        ]
    };
}
