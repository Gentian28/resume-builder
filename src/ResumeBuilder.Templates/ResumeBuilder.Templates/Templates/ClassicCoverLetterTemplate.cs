using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// The traditional full-block business letter: sender address block flush left, date, inside address,
/// subject line, salutation, body and a signature space. Serif type, no colour, no rules — the format
/// a law firm, a bank or a university expects to receive.
/// </summary>
public class ClassicCoverLetterTemplate : BaseCoverLetterTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "letter-classic",
        Name = "Classic Letter",
        Description = "Traditional block business letter in serif type — conservative and unadorned",
        Category = TemplateCategory.Classic,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "letter", "classic", "serif", "formal", "conservative" },
        DefaultAccentColor = "#111827",
        DefaultFontFamily = "Times New Roman"
    };

    protected override void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(Math.Max(Settings.PageMargin, 45)); // A business letter wants generous margins.
        page.DefaultTextStyle(x => x
            .FontSize(11 * Scale)
            .FontFamily(Settings.FontFamily)
            .FontColor(TemplateColors.Parse(Settings.TextColor)));
    }

    protected override void ComposeContent(IContainer container, CoverLetter letter)
    {
        ArgumentNullException.ThrowIfNull(letter);

        container.Column(column =>
        {
            // Sender block, flush left, as in a full-block letter.
            if (!string.IsNullOrWhiteSpace(letter.PersonalInfo.FullName))
            {
                column.Item().Text(letter.PersonalInfo.FullName)
                    .Bold()
                    .FontSize(12 * Scale)
                    .FontFamily(Settings.HeadingFontFamily);
            }

            foreach (var line in SenderContactLines(letter.PersonalInfo))
            {
                column.Item().Text(line).FontSize(10.5f * Scale);
            }

            column.Item().Height(22);

            column.Item().Text(FormatLetterDate(letter.LetterDate)).FontSize(11 * Scale);

            var recipient = RecipientLines(letter);
            if (recipient.Count > 0)
            {
                column.Item().PaddingTop(20).Column(recipientColumn =>
                {
                    foreach (var line in recipient)
                    {
                        recipientColumn.Item().Text(line).FontSize(11 * Scale);
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(letter.Subject))
            {
                column.Item().PaddingTop(20).Text(text =>
                {
                    text.Span("RE: ").Bold().FontSize(11 * Scale);
                    text.Span(letter.Subject).Bold().FontSize(11 * Scale);
                });
            }

            column.Item().PaddingTop(20).Text(letter.EffectiveSalutation).FontSize(11 * Scale);

            column.Item().PaddingTop(12).Column(bodyColumn =>
            {
                bodyColumn.Spacing(11);

                foreach (var paragraph in BodyParagraphs(letter))
                {
                    bodyColumn.Item().Text(paragraph)
                        .FontSize(11 * Scale)
                        .LineHeight(Math.Max(Settings.LineSpacing, 1.35f));
                }
            });

            column.Item().PaddingTop(22).Column(closingColumn =>
            {
                if (!string.IsNullOrWhiteSpace(letter.Closing))
                {
                    closingColumn.Item().Text(letter.Closing).FontSize(11 * Scale);
                }

                // Room for a wet signature between the closing and the typed name.
                closingColumn.Item().Height(42);

                if (!string.IsNullOrWhiteSpace(letter.SignatureName))
                {
                    closingColumn.Item().Text(letter.SignatureName).FontSize(11 * Scale);
                }

                if (!string.IsNullOrWhiteSpace(letter.PersonalInfo.JobTitle))
                {
                    closingColumn.Item().Text(letter.PersonalInfo.JobTitle)
                        .FontSize(10 * Scale)
                        .FontColor(Colors.Grey.Darken2);
                }
            });
        });
    }
}
