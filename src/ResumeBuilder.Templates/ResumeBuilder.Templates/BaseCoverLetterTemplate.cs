using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates;

/// <summary>
/// Shared plumbing for the letter templates: page setup and the recipient block. The settings are
/// taken as they stand rather than through <see cref="TemplateSettings.ApplyTemplateDefaults"/> —
/// they were resolved against the resume's template, and re-defaulting them here would undo the match
/// between a letter and the resume it accompanies.
/// </summary>
public abstract class BaseCoverLetterTemplate : ICoverLetterTemplate
{
    public abstract TemplateInfo Info { get; }

    protected TemplateSettings Settings { get; private set; } = new();

    protected float Scale => Settings.FontSizeScale;

    protected Color Accent => TemplateColors.Parse(Settings.AccentColor);

    public IDocument CreateDocument(CoverLetter letter)
    {
        ArgumentNullException.ThrowIfNull(letter);

        Settings = letter.TemplateSettings?.Clone() ?? new TemplateSettings();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Content().Element(c => ComposeContent(c, letter));
            });
        });
    }

    protected abstract void ConfigurePage(PageDescriptor page);

    protected abstract void ComposeContent(IContainer container, CoverLetter letter);

    /// <summary>Recipient name, title, company and address, one line each, blanks dropped.</summary>
    protected static List<string> RecipientLines(CoverLetter letter)
    {
        ArgumentNullException.ThrowIfNull(letter);

        var lines = new List<string>();

        void AddIfPresent(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                lines.Add(value.Trim());
        }

        AddIfPresent(letter.RecipientName);
        AddIfPresent(letter.RecipientTitle);
        AddIfPresent(letter.CompanyName);

        foreach (var line in letter.CompanyAddress.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            AddIfPresent(line);
        }

        return lines;
    }

    /// <summary>Sender contact details as one line each, blanks dropped.</summary>
    protected static List<string> SenderContactLines(PersonalInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        var address = string.Join(", ", new[] { info.Address, info.City, info.PostalCode, info.Country }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        return new[] { address, info.Phone, info.Email, info.Website }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToList();
    }

    protected static string FormatLetterDate(DateTime date) =>
        date.ToString("d MMMM yyyy", ResumeDateFormat.Culture);

    /// <summary>The paragraphs that actually carry text.</summary>
    protected static IEnumerable<string> BodyParagraphs(CoverLetter letter)
    {
        ArgumentNullException.ThrowIfNull(letter);

        return letter.Paragraphs.Where(p => !string.IsNullOrWhiteSpace(p));
    }
}
