using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export;

public static class ResumeInput
{
    public static void Normalize(Resume resume, List<string> warnings)
    {
        resume.PersonalInfo ??= new(); resume.TemplateSettings ??= new(); resume.SectionOrder ??= SectionOrder.Default;
        resume.SectionOrder.OrderedSections ??= new(); resume.SectionOrder.Visibility ??= new(); resume.SectionOrder.EnsureAllSectionsPresent();
        resume.Experiences = (resume.Experiences ?? new()).Where(e => e != null).ToList();
        resume.EducationList = (resume.EducationList ?? new()).Where(e => e != null).ToList();
        resume.Skills = (resume.Skills ?? new()).Where(e => e != null).ToList();
        resume.Languages = (resume.Languages ?? new()).Where(e => e != null).ToList();
        resume.Certifications = (resume.Certifications ?? new()).Where(e => e != null).ToList();
        resume.Projects = (resume.Projects ?? new()).Where(e => e != null).ToList();
        resume.CustomSections = (resume.CustomSections ?? new()).Where(e => e != null).ToList();
        var experienceKeys = new HashSet<Guid>();
        foreach (var experience in resume.Experiences)
            if (experience.EditKey == Guid.Empty || !experienceKeys.Add(experience.EditKey)) experience.EditKey = Guid.NewGuid();
        foreach (var e in resume.Experiences) e.Achievements = (e.Achievements ?? new()).Where(s => s != null).ToList();
        foreach (var p in resume.Projects) { p.Highlights = (p.Highlights ?? new()).Where(s => s != null).ToList(); p.Technologies = (p.Technologies ?? new()).Where(s => s != null).ToList(); }
        foreach (var c in resume.CustomSections) c.Items = (c.Items ?? new()).Where(i => i != null).ToList();
        if (resume.Experiences.Count + resume.EducationList.Count + resume.Skills.Count + resume.Languages.Count + resume.Certifications.Count + resume.Projects.Count + resume.CustomSections.Sum(c => c.Items.Count) > 10000)
            throw new InvalidDataException("The resume contains too many entries to import safely.");
        var settings = resume.TemplateSettings;
        var before = settings.Clone();
        settings.FontSizeScale = float.IsFinite(settings.FontSizeScale) ? Math.Clamp(settings.FontSizeScale, 0.6f, 2f) : 1;
        settings.PageMargin = float.IsFinite(settings.PageMargin) ? Math.Clamp(settings.PageMargin, 0, 100) : 30;
        settings.LineSpacing = float.IsFinite(settings.LineSpacing) ? Math.Clamp(settings.LineSpacing, 0.8f, 3f) : 1.4f;
        settings.SectionSpacing = float.IsFinite(settings.SectionSpacing) ? Math.Clamp(settings.SectionSpacing, 0, 60) : 15;
        if (!Enum.IsDefined(settings.PaperSize)) settings.PaperSize = PaperSize.A4;
        if (!Enum.IsDefined(settings.DocumentLanguage)) settings.DocumentLanguage = DocumentLanguage.English;
        if (before.FontSizeScale != settings.FontSizeScale || before.PageMargin != settings.PageMargin || before.LineSpacing != settings.LineSpacing || before.SectionSpacing != settings.SectionSpacing || before.PaperSize != settings.PaperSize || before.DocumentLanguage != settings.DocumentLanguage) warnings.Add("Unsupported layout settings were adjusted to safe values. Review the preview.");
        if (resume.Skills.Any(s => !Enum.IsDefined(s.Level))) { foreach (var s in resume.Skills.Where(s => !Enum.IsDefined(s.Level))) s.Level = SkillLevel.Intermediate; warnings.Add("Unknown skill levels were reset to Intermediate."); }
        if (resume.Languages.Any(l => !Enum.IsDefined(l.Proficiency))) { foreach (var l in resume.Languages.Where(l => !Enum.IsDefined(l.Proficiency))) l.Proficiency = LanguageProficiency.Professional; warnings.Add("Unknown language levels were reset to Professional."); }
    }
}
