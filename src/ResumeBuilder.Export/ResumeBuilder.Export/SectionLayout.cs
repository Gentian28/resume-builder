using ResumeBuilder.Core.Models;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Export;

/// <summary>
/// Section ordering and styling shared by the non-PDF exporters, so DOCX, HTML and text agree with
/// what the QuestPDF templates render.
/// </summary>
internal static class SectionLayout
{
    /// <summary>
    /// The resume's section order, repaired (missing sections appended, duplicates dropped) without
    /// mutating the caller's <see cref="SectionOrder"/>, and filtered to sections that have content.
    /// </summary>
    public static List<SectionType> OrderedVisibleSections(Resume resume)
    {
        var order = resume.SectionOrder ?? SectionOrder.Default;

        var seen = new HashSet<SectionType>();
        var ordered = order.OrderedSections
            .Where(s => Enum.IsDefined(s) && seen.Add(s))
            .ToList();

        ordered.AddRange(SectionOrder.AllSections.Where(s => seen.Add(s)));

        return ordered
            .Where(order.IsSectionVisible)
            .Where(s => HasContent(resume, s))
            .ToList();
    }

    public static bool HasContent(Resume resume, SectionType section) => section switch
    {
        SectionType.PersonalInfo => true,
        SectionType.Summary => !string.IsNullOrWhiteSpace(resume.Summary),
        SectionType.Experience => resume.Experiences.Any(),
        SectionType.Education => resume.EducationList.Any(),
        SectionType.Skills => resume.Skills.Any(),
        SectionType.Languages => resume.Languages.Any(),
        SectionType.Certifications => resume.Certifications.Any(),
        SectionType.Projects => resume.Projects.Any(),
        SectionType.CustomSections => VisibleCustomSections(resume).Any(),
        _ => false
    };

    public static IEnumerable<CustomSection> VisibleCustomSections(Resume resume) =>
        resume.CustomSections
            .Where(s => !string.IsNullOrWhiteSpace(s.Title) || s.Items.Any())
            .OrderBy(s => s.Order);

    /// <summary>
    /// Effective styling for a resume: the extended settings with the selected template's defaults
    /// applied for anything the user has not customized. Never mutates the resume.
    /// </summary>
    public static TemplateSettings EffectiveSettings(Resume resume, TemplateRegistry registry, string templateId)
    {
        var settings = resume.TemplateSettings?.Clone() ?? new TemplateSettings();

        var template = registry.GetTemplate(templateId) ?? registry.GetTemplate(resume.SelectedTemplateId);
        if (template != null)
            settings.ApplyTemplateDefaults(template.Info);

        return settings;
    }

    public static string CustomItemDateRange(CustomSectionItem item)
    {
        if (!item.StartDate.HasValue && !item.EndDate.HasValue)
            return string.Empty;

        if (!item.StartDate.HasValue)
            return ResumeDateFormat.MonthYear(item.EndDate);

        var start = ResumeDateFormat.MonthYear(item.StartDate);
        var end = ResumeDateFormat.MonthYear(item.EndDate);

        return string.IsNullOrEmpty(end) ? start : $"{start} - {end}";
    }

    public static string LanguageProficiencyText(LanguageProficiency level) => level switch
    {
        LanguageProficiency.Basic => "Basic",
        LanguageProficiency.Conversational => "Conversational",
        LanguageProficiency.Professional => "Professional",
        LanguageProficiency.Fluent => "Fluent",
        LanguageProficiency.Native => "Native",
        _ => "Professional"
    };

    public static string SkillLevelText(SkillLevel level) => level switch
    {
        SkillLevel.Beginner => "Beginner",
        SkillLevel.Elementary => "Elementary",
        SkillLevel.Intermediate => "Intermediate",
        SkillLevel.Advanced => "Advanced",
        SkillLevel.Expert => "Expert",
        _ => "Intermediate"
    };

    public static string CertificationDate(Certification cert)
    {
        var issued = ResumeDateFormat.MonthYear(cert.IssueDate);

        if (cert.DoesNotExpire || !cert.ExpirationDate.HasValue)
            return issued;

        var expires = ResumeDateFormat.MonthYear(cert.ExpirationDate);
        return string.IsNullOrEmpty(issued) ? $"expires {expires}" : $"{issued} - {expires}";
    }
}
