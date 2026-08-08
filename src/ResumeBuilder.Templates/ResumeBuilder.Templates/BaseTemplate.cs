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
        CurrentResume = resume;

        // Work on a copy: applying this template's defaults must not write back to the caller's resume.
        Settings = resume.TemplateSettings?.Clone() ?? new TemplateSettings();
        Settings.ApplyTemplateDefaults(Info);

        _orderedSections = BuildOrderedSections(resume.SectionOrder);

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

        var startStr = ResumeDateFormat.MonthYear(start);
        var endStr = isCurrent ? "Present" : ResumeDateFormat.MonthYear(end);

        return $"{startStr} - {endStr}";
    }

    protected static string FormatYearRange(DateTime? start, DateTime? end, bool isCurrent)
    {
        if (!start.HasValue) return string.Empty;

        var startStr = ResumeDateFormat.Year(start);
        var endStr = isCurrent ? "Present" : ResumeDateFormat.Year(end);

        return $"{startStr} - {endStr}";
    }

    protected Color ParseColor(string hex) => TemplateColors.Parse(hex);

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

    protected static string FormatLanguage(Language language, string separator = " - ") =>
        $"{language.Name}{separator}{GetLanguageProficiencyText(language.Proficiency)}";

    /// <summary>Skill level as a 0-100 fill percentage for progress bars.</summary>
    protected static int GetSkillPercent(SkillLevel level) => Math.Clamp((int)level * 20, 0, 100);

    /// <summary>
    /// Horizontal skill bar. The remainder item is omitted at 100% because QuestPDF treats
    /// RelativeItem(0) as an error rather than a zero-width item.
    /// </summary>
    protected void ComposeSkillBar(IContainer container, SkillLevel level, Color fillColor, Color trackColor, float height = 4)
    {
        var percent = GetSkillPercent(level);

        container.Height(height).Background(trackColor).Row(bar =>
        {
            if (percent > 0)
                bar.RelativeItem(percent).Background(fillColor);

            if (percent < 100)
                bar.RelativeItem(100 - percent);
        });
    }

    /// <summary>
    /// Display text for a profile link: strips the scheme and a known host prefix so an imported
    /// "https://github.com/user" renders as "user" rather than "github.com/https://github.com/user".
    /// </summary>
    protected static string FormatProfileHandle(string? value, string host)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim();

        foreach (var scheme in new[] { "https://", "http://" })
        {
            if (text.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                text = text[scheme.Length..];
                break;
            }
        }

        if (text.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            text = text[4..];

        if (text.StartsWith(host, StringComparison.OrdinalIgnoreCase))
            text = text[host.Length..];

        return text.Trim('/');
    }

    protected static string FormatGitHubDisplay(string? value) => FormatProfileHandle(value, "github.com");

    /// <summary>
    /// Display text for a LinkedIn link as a full typeable address: "https://www.linkedin.com/in/user/"
    /// renders as "linkedin.com/in/user" — same convention as the "github.com/user" call sites.
    /// </summary>
    protected static string FormatLinkedInDisplay(string? value)
    {
        var handle = FormatProfileHandle(value, "linkedin.com");
        return string.IsNullOrEmpty(handle) ? string.Empty : $"linkedin.com/{handle}";
    }

    /// <summary>
    /// Display text for the personal website: strips the scheme, "www." and a trailing slash so
    /// "https://www.example.com/" renders as "example.com". Public because cover-letter templates
    /// implement ICoverLetterTemplate directly and do not extend this class.
    /// </summary>
    public static string FormatWebsiteDisplay(string? value) => FormatProfileHandle(value, string.Empty);

    // Photo rendering helper
    protected void ComposePhotoOrInitials(IContainer container, Resume resume, float size, Color backgroundColor, Color initialsColor)
    {
        var photo = resume.PersonalInfo.Photo;
        if (photo != null && photo.Length > 0)
        {
            container
                .Width(size)
                .Height(size)
                .Image(photo)
                .FitArea();
        }
        else
        {
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

    private List<SectionType> _orderedSections = SectionOrder.AllSections.ToList();

    protected bool IsSectionVisible(SectionType sectionType)
    {
        return CurrentResume?.SectionOrder?.IsSectionVisible(sectionType) ?? true;
    }

    protected IEnumerable<SectionType> GetOrderedSections() => _orderedSections;

    /// <summary>Ordered sections that actually have something to show, for templates that need to look ahead.</summary>
    protected List<SectionType> GetVisibleSections(Resume resume) =>
        _orderedSections.Where(s => ShouldRenderSection(s, resume)).ToList();

    /// <summary>
    /// Repairs a persisted order (missing or duplicated sections) without touching the caller's
    /// <see cref="SectionOrder"/>, so a resume saved before a section existed still renders it.
    /// </summary>
    private static List<SectionType> BuildOrderedSections(SectionOrder? sectionOrder)
    {
        if (sectionOrder == null)
            return SectionOrder.AllSections.ToList();

        var seen = new HashSet<SectionType>();
        var ordered = sectionOrder.OrderedSections
            .Where(s => Enum.IsDefined(s) && seen.Add(s))
            .ToList();

        ordered.AddRange(SectionOrder.AllSections.Where(s => seen.Add(s)));
        return ordered;
    }

    protected bool ShouldRenderSection(SectionType sectionType, Resume resume)
    {
        if (!IsSectionVisible(sectionType))
            return false;

        return sectionType switch
        {
            SectionType.PersonalInfo => true,
            SectionType.Summary => !string.IsNullOrWhiteSpace(resume.Summary),
            SectionType.Experience => resume.Experiences.Any(),
            SectionType.Education => resume.EducationList.Any(),
            SectionType.Skills => resume.Skills.Any(),
            SectionType.Languages => resume.Languages.Any(),
            SectionType.Certifications => resume.Certifications.Any(),
            SectionType.Projects => resume.Projects.Any(),
            SectionType.CustomSections => GetVisibleCustomSections(resume).Any(),
            _ => false
        };
    }

    protected static IEnumerable<CustomSection> GetVisibleCustomSections(Resume resume) =>
        resume.CustomSections
            .Where(s => !string.IsNullOrWhiteSpace(s.Title) || s.Items.Any())
            .OrderBy(s => s.Order);

    /// <summary>
    /// Renders the items of a custom section. Callers supply their own section heading so each
    /// template keeps its own section chrome.
    /// </summary>
    protected void ComposeCustomSectionItems(ColumnDescriptor column, CustomSection section, float titleFontSize = 10, float bodyFontSize = 9)
    {
        foreach (var item in section.Items.OrderBy(i => i.Order))
        {
            column.Item().PaddingBottom(6).Column(itemCol =>
            {
                itemCol.Item().Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span(item.Title).Bold().FontSize(titleFontSize * FontSizeScale);
                        if (!string.IsNullOrWhiteSpace(item.Subtitle))
                        {
                            text.Span($"  |  {item.Subtitle}")
                                .FontSize(bodyFontSize * FontSizeScale)
                                .FontColor(Colors.Grey.Darken1);
                        }
                    });

                    var dateRange = FormatCustomItemDateRange(item);
                    if (!string.IsNullOrEmpty(dateRange))
                    {
                        row.AutoItem().Text(dateRange)
                            .FontSize(bodyFontSize * FontSizeScale)
                            .FontColor(Colors.Grey.Darken1);
                    }
                });

                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    itemCol.Item().PaddingTop(2).Text(item.Description)
                        .FontSize(bodyFontSize * FontSizeScale)
                        .LineHeight(LineSpacing);
                }
            });
        }
    }

    protected static string FormatCustomItemDateRange(CustomSectionItem item)
    {
        if (!item.StartDate.HasValue && !item.EndDate.HasValue)
            return string.Empty;

        if (!item.StartDate.HasValue)
            return ResumeDateFormat.MonthYear(item.EndDate);

        var start = ResumeDateFormat.MonthYear(item.StartDate);
        var end = ResumeDateFormat.MonthYear(item.EndDate);

        return string.IsNullOrEmpty(end) ? start : $"{start} - {end}";
    }

    /// <summary>
    /// The one separator for pipe-joined strips (contact lines, link lines, language rows).
    /// Templates that want a different glyph (·, •, ◆) keep their own, but every "|" must use
    /// this so the pipes read identically across lines and templates.
    /// </summary>
    public const string ContactSeparator = "  |  ";

    // Shared entry blocks. Styles let a template keep its own type scale and accents while
    // sharing the layout that is otherwise copy-pasted between templates.
    protected sealed record EntryStyle
    {
        public float TitleFontSize { get; init; } = 11;
        public float SubtitleFontSize { get; init; } = 10;
        public float BodyFontSize { get; init; } = 9;
        public float MetaFontSize { get; init; } = 9;
        public Color? SubtitleColor { get; init; }
        public string LocationSeparator { get; init; } = "  |  ";
        public string Bullet { get; init; } = "• ";
    }

    protected void ComposeExperienceEntry(IContainer container, Experience exp, EntryStyle style)
    {
        container.EnsureSpace(48).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(exp.JobTitle).Bold().FontSize(style.TitleFontSize * FontSizeScale);
                row.AutoItem().Text(exp.DateRange).FontSize(style.MetaFontSize * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(text =>
            {
                var company = text.Span(exp.Company).SemiBold().FontSize(style.SubtitleFontSize * FontSizeScale);
                if (style.SubtitleColor.HasValue)
                    company.FontColor(style.SubtitleColor.Value);

                if (!string.IsNullOrWhiteSpace(exp.Location))
                {
                    text.Span($"{style.LocationSeparator}{exp.Location}")
                        .FontSize(style.BodyFontSize * FontSizeScale)
                        .FontColor(Colors.Grey.Darken1);
                }
            });

            if (!string.IsNullOrWhiteSpace(exp.Description))
            {
                column.Item().PaddingTop(4).Text(exp.Description)
                    .FontSize(style.BodyFontSize * FontSizeScale).LineHeight(LineSpacing);
            }

            if (exp.Achievements.Any())
                column.Item().PaddingTop(4).Element(c => ComposeBulletList(c, exp.Achievements, style));
        });
    }

    protected void ComposeEducationEntry(IContainer container, Education edu, EntryStyle style)
    {
        container.EnsureSpace(48).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(edu.DegreeWithField).Bold().FontSize(style.TitleFontSize * FontSizeScale);
                row.AutoItem().Text(edu.DateRange).FontSize(style.MetaFontSize * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(text =>
            {
                var institution = text.Span(edu.Institution).SemiBold().FontSize(style.SubtitleFontSize * FontSizeScale);
                if (style.SubtitleColor.HasValue)
                    institution.FontColor(style.SubtitleColor.Value);

                if (!string.IsNullOrWhiteSpace(edu.Location))
                {
                    text.Span($"{style.LocationSeparator}{edu.Location}")
                        .FontSize(style.BodyFontSize * FontSizeScale)
                        .FontColor(Colors.Grey.Darken1);
                }
            });

            if (!string.IsNullOrWhiteSpace(edu.Grade))
            {
                column.Item().Text($"Grade: {edu.Grade}")
                    .FontSize(style.BodyFontSize * FontSizeScale).FontColor(Colors.Grey.Darken1);
            }

            if (!string.IsNullOrWhiteSpace(edu.Description))
            {
                column.Item().PaddingTop(2).Text(edu.Description)
                    .FontSize(style.BodyFontSize * FontSizeScale).LineHeight(LineSpacing);
            }
        });
    }

    protected void ComposeProjectEntry(IContainer container, Project project, EntryStyle style)
    {
        container.EnsureSpace(48).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(project.Name).Bold().FontSize(style.TitleFontSize * FontSizeScale);
                if (project.StartDate.HasValue)
                {
                    row.AutoItem().Text(FormatDateRange(project.StartDate, project.EndDate, project.IsOngoing))
                        .FontSize(style.MetaFontSize * FontSizeScale).FontColor(Colors.Grey.Darken1);
                }
            });

            if (!string.IsNullOrWhiteSpace(project.Description))
            {
                column.Item().PaddingTop(2).Text(project.Description)
                    .FontSize(style.BodyFontSize * FontSizeScale).LineHeight(LineSpacing);
            }

            if (!string.IsNullOrWhiteSpace(project.Url))
            {
                column.Item().PaddingTop(2).Text(project.Url)
                    .FontSize(style.BodyFontSize * FontSizeScale).FontColor(Colors.Grey.Darken1);
            }

            if (project.Technologies.Any())
            {
                column.Item().PaddingTop(2).Text(text =>
                {
                    text.Span("Technologies: ").Bold().FontSize(style.BodyFontSize * FontSizeScale);
                    text.Span(string.Join(", ", project.Technologies)).FontSize(style.BodyFontSize * FontSizeScale);
                });
            }

            if (project.Highlights.Any())
                column.Item().PaddingTop(3).Element(c => ComposeBulletList(c, project.Highlights, style));
        });
    }

    protected void ComposeBulletList(IContainer container, IEnumerable<string> items, EntryStyle style)
    {
        // The gap is padding, not a trailing space in the glyph: QuestPDF collapses trailing
        // whitespace, which would butt the bullet against the text.
        var bullet = style.Bullet.TrimEnd();

        container.Column(column =>
        {
            foreach (var item in items)
            {
                column.Item().Row(row =>
                {
                    row.AutoItem().PaddingRight(5).Text(bullet).FontSize(style.BodyFontSize * FontSizeScale);
                    row.RelativeItem().Text(item)
                        .FontSize(style.BodyFontSize * FontSizeScale).LineHeight(LineSpacing);
                });
            }
        });
    }
}
