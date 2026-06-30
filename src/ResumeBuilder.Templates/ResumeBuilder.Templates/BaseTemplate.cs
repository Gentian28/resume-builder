using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates;

public abstract class BaseTemplate : IResumeTemplate
{
    public abstract TemplateInfo Info { get; }

    protected TemplateSettings Settings { get; private set; } = new();

    // Convenience accessors for commonly used settings
    protected string AccentColor => Settings.AccentColor;
    protected string SecondaryColor => Settings.SecondaryColor;
    protected string TextColor => Settings.TextColor;
    protected string HeadingColor => Settings.HeadingColor;
    protected string FontFamily => Settings.FontFamily;
    protected string HeadingFontFamily => Settings.HeadingFontFamily;
    protected float FontSizeScale => Settings.FontSizeScale;
    protected float LineSpacing => Settings.LineSpacing;
    protected float SectionSpacing => Settings.SectionSpacing;
    protected float PageMargin => Settings.PageMargin;

    public IDocument CreateDocument(Resume resume)
    {
        // Store resume reference for section ordering
        CurrentResume = resume;

        // Use extended settings, fallback to legacy properties for backwards compatibility
        Settings = resume.TemplateSettings ?? new TemplateSettings
        {
            AccentColor = resume.AccentColor,
            FontFamily = resume.FontFamily
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Content().Element(c => ComposeContent(c, resume));
            });
        });
    }

    protected virtual void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(PageMargin);
        page.DefaultTextStyle(x => x
            .FontSize(10 * FontSizeScale)
            .FontFamily(FontFamily)
            .FontColor(ParseColor(TextColor)));
    }

    protected abstract void ComposeContent(IContainer container, Resume resume);

    // Helper methods for common formatting
    protected static string FormatDateRange(DateTime? start, DateTime? end, bool isCurrent)
    {
        if (!start.HasValue) return string.Empty;

        var startStr = start.Value.ToString("MMM yyyy");
        var endStr = isCurrent ? "Present" : (end?.ToString("MMM yyyy") ?? "");

        return $"{startStr} - {endStr}";
    }

    protected static string FormatYearRange(DateTime? start, DateTime? end, bool isCurrent)
    {
        if (!start.HasValue) return string.Empty;

        var startStr = start.Value.ToString("yyyy");
        var endStr = isCurrent ? "Present" : (end?.ToString("yyyy") ?? "");

        return $"{startStr} - {endStr}";
    }

    protected Color ParseColor(string hex)
    {
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length != 7)
            return Colors.Blue.Medium;

        try
        {
            var r = Convert.ToByte(hex.Substring(1, 2), 16);
            var g = Convert.ToByte(hex.Substring(3, 2), 16);
            var b = Convert.ToByte(hex.Substring(5, 2), 16);
            return Color.FromRGB(r, g, b);
        }
        catch
        {
            return Colors.Blue.Medium;
        }
    }

    protected static string GetSkillLevelText(SkillLevel level) => level switch
    {
        SkillLevel.Beginner => "Beginner",
        SkillLevel.Elementary => "Elementary",
        SkillLevel.Intermediate => "Intermediate",
        SkillLevel.Advanced => "Advanced",
        SkillLevel.Expert => "Expert",
        _ => "Intermediate"
    };

    protected static string GetLanguageProficiencyText(LanguageProficiency level) => level switch
    {
        LanguageProficiency.Basic => "Basic",
        LanguageProficiency.Conversational => "Conversational",
        LanguageProficiency.Professional => "Professional",
        LanguageProficiency.Fluent => "Fluent",
        LanguageProficiency.Native => "Native",
        _ => "Professional"
    };

    // Photo rendering helper
    protected void ComposePhotoOrInitials(IContainer container, Resume resume, float size, Color backgroundColor, Color initialsColor)
    {
        var photo = resume.PersonalInfo.Photo;
        if (photo != null && photo.Length > 0)
        {
            // Render actual photo
            container
                .Width(size)
                .Height(size)
                .Image(photo, ImageScaling.FitArea);
        }
        else
        {
            // Render initials as fallback
            container
                .Width(size)
                .Height(size)
                .Background(backgroundColor)
                .AlignCenter()
                .AlignMiddle()
                .Text(GetInitials(resume.PersonalInfo))
                .FontSize(size * 0.35f)
                .Bold()
                .FontColor(initialsColor);
        }
    }

    protected static string GetInitials(PersonalInfo info)
    {
        var first = !string.IsNullOrEmpty(info.FirstName) && info.FirstName.Length > 0
            ? info.FirstName[0].ToString().ToUpper() : "";
        var last = !string.IsNullOrEmpty(info.LastName) && info.LastName.Length > 0
            ? info.LastName[0].ToString().ToUpper() : "";
        return first + last;
    }

    // Section ordering helpers
    protected Resume? CurrentResume { get; private set; }

    protected bool IsSectionVisible(SectionType sectionType)
    {
        return CurrentResume?.SectionOrder?.IsSectionVisible(sectionType) ?? true;
    }

    protected IEnumerable<SectionType> GetOrderedSections()
    {
        return CurrentResume?.SectionOrder?.OrderedSections ?? SectionOrder.Default.OrderedSections;
    }

    protected bool ShouldRenderSection(SectionType sectionType, Resume resume)
    {
        if (!IsSectionVisible(sectionType))
            return false;

        return sectionType switch
        {
            SectionType.PersonalInfo => true, // Always render header
            SectionType.Summary => !string.IsNullOrWhiteSpace(resume.Summary),
            SectionType.Experience => resume.Experiences.Any(),
            SectionType.Education => resume.EducationList.Any(),
            SectionType.Skills => resume.Skills.Any(),
            SectionType.Languages => resume.Languages.Any(),
            SectionType.Certifications => resume.Certifications.Any(),
            SectionType.Projects => resume.Projects.Any(),
            _ => false
        };
    }
}
