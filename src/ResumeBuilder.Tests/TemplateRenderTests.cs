using FluentAssertions;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Tests;

/// <summary>
/// Renders every registered template against a fully populated resume. Nothing exercised the
/// templates individually before, which is how bugs like the Executive template silently dropping
/// skill categories survived. A new template is covered as soon as it is registered.
/// </summary>
public class TemplateRenderTests
{
    private readonly TemplateRegistry _registry = new();

    public TemplateRenderTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static TheoryData<string> AllTemplateIds()
    {
        var data = new TheoryData<string>();
        foreach (var template in new TemplateRegistry().GetAllTemplates())
        {
            data.Add(template.Info.Id);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AllTemplateIds))]
    public async Task Template_RendersFullResumeToPdf(string templateId)
    {
        var service = new ExportService(_registry);
        var resume = TestResumes.FullyPopulated();
        resume.SelectedTemplateId = templateId;

        var pdf = await service.ExportAsync(resume, "PDF");

        pdf.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(pdf!, 0, 4).Should().Be("%PDF");
    }

    [Theory]
    [MemberData(nameof(AllTemplateIds))]
    public async Task Template_RendersEmptyResumeWithoutThrowing(string templateId)
    {
        var service = new ExportService(_registry);
        var resume = new Resume { SelectedTemplateId = templateId };

        var pdf = await service.ExportAsync(resume, "PDF");

        pdf.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [MemberData(nameof(AllTemplateIds))]
    public async Task Template_HidingEverySection_StillRenders(string templateId)
    {
        var service = new ExportService(_registry);
        var resume = TestResumes.FullyPopulated();
        resume.SelectedTemplateId = templateId;

        foreach (var section in SectionOrder.AllSections)
        {
            resume.SectionOrder.SetSectionVisibility(section, false);
        }

        var pdf = await service.ExportAsync(resume, "PDF");

        pdf.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// The shared fixture carries no photo, so the photo path went unexercised until the Photo Header
    /// and European CV templates started drawing one.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllTemplateIds))]
    public async Task Template_RendersAResumeWithAPhoto(string templateId)
    {
        var service = new ExportService(_registry);
        var resume = TestResumes.FullyPopulated();
        resume.SelectedTemplateId = templateId;
        resume.PersonalInfo.Photo = TestResumes.SamplePhoto();

        var pdf = await service.ExportAsync(resume, "PDF");

        pdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void EveryTemplate_DeclaresDistinctIdAndDefaults()
    {
        var templates = _registry.GetAllTemplates().ToList();

        templates.Should().HaveCountGreaterThan(1);
        templates.Select(t => t.Info.Id).Should().OnlyHaveUniqueItems();
        templates.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.Info.Name));
        templates.Should().OnlyContain(t => t.Info.DefaultAccentColor.StartsWith("#"));
        templates.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.Info.DefaultFontFamily));
    }

    [Fact]
    public void TemplateDefaults_ApplyWhenTheUserHasNotCustomized()
    {
        var settings = new TemplateSettings();
        var info = _registry.GetTemplate("classic")!.Info;

        settings.ApplyTemplateDefaults(info);

        settings.AccentColor.Should().Be(info.DefaultAccentColor);
        settings.FontFamily.Should().Be(info.DefaultFontFamily);
    }

    [Fact]
    public void TemplateDefaults_DoNotOverrideAUserChoice()
    {
        var settings = new TemplateSettings
        {
            AccentColor = "#ff0000",
            IsAccentColorCustomized = true,
            FontFamily = "Georgia",
            IsFontCustomized = true
        };

        settings.ApplyTemplateDefaults(_registry.GetTemplate("classic")!.Info);

        settings.AccentColor.Should().Be("#ff0000");
        settings.FontFamily.Should().Be("Georgia");
    }
}

internal static class TestResumes
{
    /// <summary>A 2x2 PNG — enough for a template to have a real image to lay out.</summary>
    public static byte[] SamplePhoto() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAFElEQVR4nGP8z8DAwMDAwMQAAgAOEwEB1eiJUAAAAABJRU5ErkJggg==");

    /// <summary>A resume with every section populated, including the ones templates used to skip.</summary>
    public static Resume FullyPopulated() => new()
    {
        Name = "Full Resume",
        SelectedTemplateId = "modern",
        PersonalInfo = new PersonalInfo
        {
            FirstName = "Jane",
            LastName = "Doe",
            JobTitle = "Software Engineer",
            Email = "jane.doe@example.com",
            Phone = "+1 555 0100",
            City = "Springfield",
            Country = "USA",
            Website = "https://example.com",
            LinkedIn = "https://www.linkedin.com/in/jane-doe/",
            GitHub = "https://github.com/janedoe"
        },
        Summary = "Backend engineer with eight years of experience across fintech and SaaS.",
        Experiences =
        {
            new Experience
            {
                Order = 0,
                JobTitle = "Senior Engineer",
                Company = "Northwind",
                Location = "Springfield, IL",
                StartDate = new DateTime(2022, 3, 1),
                IsCurrentRole = true,
                Description = "Data platform team.",
                Achievements = { "Cut processing from 18 hours to 5 minutes", "Mentored three engineers" }
            },
            new Experience
            {
                Order = 1,
                JobTitle = "Engineer",
                Company = "Contoso",
                StartDate = new DateTime(2019, 1, 1),
                EndDate = new DateTime(2022, 2, 1),
                Description = "Payments.",
                Achievements = { "Split the payments monolith", "Built a shared cache library" }
            }
        },
        EducationList =
        {
            new Education
            {
                Degree = "BSc",
                FieldOfStudy = "Computer Science",
                Institution = "State University",
                Location = "Springfield, IL",
                StartDate = new DateTime(2013, 9, 1),
                EndDate = new DateTime(2017, 5, 1),
                Grade = "3.8",
                Description = "Focus on distributed systems."
            }
        },

        // More than three categories: the Executive template used to silently drop the rest.
        Skills =
        {
            new Skill { Name = "C#", Level = SkillLevel.Expert, Category = "Languages" },
            new Skill { Name = "SQL", Level = SkillLevel.Advanced, Category = "Databases" },
            new Skill { Name = "AWS", Level = SkillLevel.Advanced, Category = "Cloud" },
            new Skill { Name = "React", Level = SkillLevel.Intermediate, Category = "Frontend" },
            new Skill { Name = "Leadership", Level = SkillLevel.Advanced, Category = "Soft Skills" }
        },
        Languages =
        {
            new Language { Name = "English", Proficiency = LanguageProficiency.Native },
            new Language { Name = "Spanish", Proficiency = LanguageProficiency.Conversational }
        },
        Certifications =
        {
            new Certification
            {
                Name = "AWS Solutions Architect",
                IssuingOrganization = "Amazon",
                IssueDate = new DateTime(2023, 4, 1),
                DoesNotExpire = true,
                CredentialId = "ABC-123",
                CredentialUrl = "https://example.com/cert"
            }
        },
        Projects =
        {
            new Project
            {
                Name = "Open Source Tool",
                Description = "A CLI for schema diffs.",
                Url = "https://github.com/janedoe/tool",
                StartDate = new DateTime(2021, 1, 1),
                IsOngoing = true,
                Technologies = { "C#", "SQLite" },
                Highlights = { "1k stars", "Used by three companies" }
            }
        },
        CustomSections =
        {
            new CustomSection
            {
                Title = "Speaking",
                Items =
                {
                    new CustomSectionItem
                    {
                        Title = "Scaling Ingestion Pipelines",
                        Subtitle = "DevConf",
                        Description = "Talk on pipeline design.",
                        StartDate = new DateTime(2024, 6, 1)
                    }
                }
            }
        }
    };
}
