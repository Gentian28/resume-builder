using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// A standard business letter: sender block, date, recipient block, subject, salutation, body and a
/// signed closing. Styled from the letter's own <see cref="TemplateSettings"/>, which
/// <see cref="CoverLetter.FromResume"/> copies off the resume, so the pair looks like one set.
/// </summary>
public class CoverLetterTemplate : ICoverLetterTemplate
{
    public TemplateInfo Info => new()
    {
        Id = "letter-modern",
        Name = "Business Letter",
        Description = "Standard business letter styled to match the resume it accompanies",
        Category = TemplateCategory.Professional,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "letter", "business", "formal" },
        DefaultAccentColor = TemplateSettings.DefaultAccentColor,
        DefaultFontFamily = TemplateSettings.DefaultFontFamily
    };

    private TemplateSettings _settings = new();

    public IDocument CreateDocument(CoverLetter letter)
    {
        // The settings are taken as they stand rather than through ApplyTemplateDefaults: they were
        // resolved against the resume's template, and re-defaulting them here would undo the match.
        _settings = letter.TemplateSettings?.Clone() ?? new TemplateSettings();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(_settings.PageMargin);
                page.DefaultTextStyle(x => x
                    .FontSize(10 * _settings.FontSizeScale)
                    .FontFamily(_settings.FontFamily)
                    .FontColor(TemplateColors.Parse(_settings.TextColor)));

                page.Content().Element(c => ComposeContent(c, letter));
            });
        });
    }

    private void ComposeContent(IContainer container, CoverLetter letter)
    {
        var accent = TemplateColors.Parse(_settings.AccentColor);
        var scale = _settings.FontSizeScale;

        container.Column(column =>
        {
            column.Spacing(_settings.SectionSpacing);

            column.Item().Element(c => ComposeSender(c, letter.PersonalInfo, accent));

            column.Item().PaddingTop(2).LineHorizontal(1).LineColor(accent);

            column.Item().Text(letter.LetterDate.ToString("d MMMM yyyy", ResumeDateFormat.Culture))
                .FontSize(10 * scale)
                .FontColor(Colors.Grey.Darken1);

            var recipient = RecipientLines(letter);
            if (recipient.Count > 0)
            {
                column.Item().Column(recipientColumn =>
                {
                    foreach (var line in recipient)
                    {
                        recipientColumn.Item().Text(line).FontSize(10 * scale);
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(letter.Subject))
            {
                column.Item().Text(letter.Subject)
                    .Bold()
                    .FontSize(11 * scale)
                    .FontFamily(_settings.HeadingFontFamily)
                    .FontColor(TemplateColors.Parse(_settings.HeadingColor));
            }

            column.Item().Text(letter.EffectiveSalutation).FontSize(10 * scale);

            column.Item().Column(bodyColumn =>
            {
                bodyColumn.Spacing(8);

                foreach (var paragraph in letter.Paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    bodyColumn.Item().Text(paragraph)
                        .FontSize(10 * scale)
                        .LineHeight(_settings.LineSpacing);
                }
            });

            column.Item().Element(c => ComposeClosing(c, letter, scale));
        });
    }

    private void ComposeSender(IContainer container, PersonalInfo info, Color accent)
    {
        var scale = _settings.FontSizeScale;

        container.Column(column =>
        {
            if (!string.IsNullOrWhiteSpace(info.FullName))
            {
                column.Item().Text(info.FullName)
                    .Bold()
                    .FontSize(20 * scale)
                    .FontFamily(_settings.HeadingFontFamily)
                    .FontColor(accent);
            }

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
            {
                column.Item().Text(info.JobTitle)
                    .FontSize(11 * scale)
                    .FontColor(TemplateColors.Parse(_settings.SecondaryColor));
            }

            var contacts = new[] { info.Email, info.Phone, info.Location, info.Website }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            if (contacts.Count > 0)
            {
                column.Item().PaddingTop(3).Text(string.Join("  |  ", contacts))
                    .FontSize(9 * scale)
                    .FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private static List<string> RecipientLines(CoverLetter letter)
    {
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

    private void ComposeClosing(IContainer container, CoverLetter letter, float scale)
    {
        container.Column(column =>
        {
            if (!string.IsNullOrWhiteSpace(letter.Closing))
            {
                column.Item().Text(letter.Closing).FontSize(10 * scale);
            }

            // Blank space for a handwritten signature between the closing and the typed name.
            column.Item().Height(28);

            if (!string.IsNullOrWhiteSpace(letter.SignatureName))
            {
                column.Item().Text(letter.SignatureName)
                    .SemiBold()
                    .FontSize(11 * scale)
                    .FontFamily(_settings.HeadingFontFamily);
            }
        });
    }
}
