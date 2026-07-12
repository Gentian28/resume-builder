using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// The spare, modern letter: a letterspaced name, one hairline of accent, and then a great deal of
/// air around a short body. Everything that can be implied is left out — no "RE:", no boxed blocks —
/// on the assumption the letter is three paragraphs and wants to look like it.
/// </summary>
public class MinimalCoverLetterTemplate : BaseCoverLetterTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "letter-minimal",
        Name = "Minimal Letter",
        Description = "Spare modern letter with generous whitespace and a single accent hairline",
        Category = TemplateCategory.Modern,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "letter", "minimal", "modern", "whitespace" },
        DefaultAccentColor = "#0f766e",
        DefaultFontFamily = "Calibri"
    };

    protected override void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(Math.Max(Settings.PageMargin, 55)); // The whitespace is the design.
        page.DefaultTextStyle(x => x
            .FontSize(10 * Scale)
            .FontFamily(Settings.FontFamily)
            .FontColor(TemplateColors.Parse(Settings.TextColor)));
    }

    protected override void ComposeContent(IContainer container, CoverLetter letter)
    {
        ArgumentNullException.ThrowIfNull(letter);

        container.Column(column =>
        {
            var info = letter.PersonalInfo;

            if (!string.IsNullOrWhiteSpace(info.FullName))
            {
                column.Item().Text(info.FullName.ToUpperInvariant())
                    .FontSize(15 * Scale)
                    .LetterSpacing(0.25f)
                    .FontFamily(Settings.HeadingFontFamily)
                    .FontColor(TemplateColors.Parse(Settings.HeadingColor));
            }

            var contacts = SenderContactLines(info);
            if (contacts.Count > 0)
            {
                column.Item().PaddingTop(6).Text(string.Join("   ·   ", contacts))
                    .FontSize(8.5f * Scale)
                    .FontColor(Colors.Grey.Darken1);
            }

            column.Item().PaddingTop(14).Width(48).Height(2).Background(Accent);

            column.Item().PaddingTop(38).Text(FormatLetterDate(letter.LetterDate))
                .FontSize(9 * Scale)
                .FontColor(Colors.Grey.Darken1);

            var recipient = RecipientLines(letter);
            if (recipient.Count > 0)
            {
                column.Item().PaddingTop(20).Column(recipientColumn =>
                {
                    foreach (var line in recipient)
                    {
                        recipientColumn.Item().Text(line)
                            .FontSize(9.5f * Scale)
                            .LineHeight(1.4f);
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(letter.Subject))
            {
                column.Item().PaddingTop(28).Text(letter.Subject)
                    .FontSize(12 * Scale)
                    .SemiBold()
                    .FontFamily(Settings.HeadingFontFamily)
                    .FontColor(Accent);
            }

            column.Item().PaddingTop(24).Text(letter.EffectiveSalutation).FontSize(10 * Scale);

            column.Item().PaddingTop(14).Column(bodyColumn =>
            {
                bodyColumn.Spacing(14);

                foreach (var paragraph in BodyParagraphs(letter))
                {
                    bodyColumn.Item().Text(paragraph)
                        .FontSize(10 * Scale)
                        .LineHeight(Math.Max(Settings.LineSpacing, 1.55f));
                }
            });

            column.Item().PaddingTop(34).Column(closingColumn =>
            {
                if (!string.IsNullOrWhiteSpace(letter.Closing))
                {
                    closingColumn.Item().Text(letter.Closing)
                        .FontSize(10 * Scale)
                        .FontColor(Colors.Grey.Darken2);
                }

                closingColumn.Item().Height(26);

                if (!string.IsNullOrWhiteSpace(letter.SignatureName))
                {
                    closingColumn.Item().Text(letter.SignatureName)
                        .FontSize(11 * Scale)
                        .SemiBold()
                        .FontFamily(Settings.HeadingFontFamily)
                        .FontColor(TemplateColors.Parse(Settings.HeadingColor));
                }
            });
        });
    }
}
