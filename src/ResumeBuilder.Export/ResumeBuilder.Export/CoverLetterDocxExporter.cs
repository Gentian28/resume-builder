using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ResumeBuilder.Core.Models;
using WColor = DocumentFormat.OpenXml.Wordprocessing.Color;

namespace ResumeBuilder.Export;

public class CoverLetterDocxExporter : ICoverLetterExporter
{
    public string Format => "DOCX";
    public string FileExtension => ".docx";
    public string MimeType => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public Task<byte[]> ExportAsync(CoverLetter letter, string templateId)
    {
        var settings = letter.TemplateSettings ?? new TemplateSettings();
        var accentColor = DocxExporter.NormalizeColor(settings.AccentColor, TemplateSettings.DefaultAccentColor);
        var fontFamily = string.IsNullOrWhiteSpace(settings.FontFamily)
            ? TemplateSettings.DefaultFontFamily
            : settings.FontFamily;

        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            ApplyDefaultFont(mainPart, fontFamily);

            AddSender(body, letter.PersonalInfo, accentColor);
            AddParagraph(body, letter.LetterDate.ToString("d MMMM yyyy", ResumeDateFormat.Culture));

            foreach (var line in RecipientLines(letter))
            {
                AddParagraph(body, line, spacingAfter: "0");
            }

            if (!string.IsNullOrWhiteSpace(letter.Subject))
            {
                AddParagraph(body, letter.Subject, spacingBefore: "200", bold: true);
            }

            AddParagraph(body, letter.EffectiveSalutation, spacingBefore: "200");

            foreach (var paragraph in letter.Paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                AddParagraph(body, paragraph.Trim(), spacingAfter: "160");
            }

            if (!string.IsNullOrWhiteSpace(letter.Closing))
            {
                AddParagraph(body, letter.Closing, spacingBefore: "200", spacingAfter: "0");
            }

            // An empty paragraph leaves room for a handwritten signature above the typed name.
            body.AppendChild(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "200" })));

            if (!string.IsNullOrWhiteSpace(letter.SignatureName))
            {
                AddParagraph(body, letter.SignatureName, bold: true);
            }
        }

        return Task.FromResult(stream.ToArray());
    }

    public async Task ExportToFileAsync(CoverLetter letter, string templateId, string filePath)
    {
        var bytes = await ExportAsync(letter, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    private static List<string> RecipientLines(CoverLetter letter)
    {
        var lines = new List<string>();

        foreach (var value in new[] { letter.RecipientName, letter.RecipientTitle, letter.CompanyName })
        {
            if (!string.IsNullOrWhiteSpace(value))
                lines.Add(value.Trim());
        }

        foreach (var line in letter.CompanyAddress.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line.Trim());
        }

        return lines;
    }

    private static void ApplyDefaultFont(MainDocumentPart mainPart, string fontFamily)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunPropertiesBaseStyle(
                        new RunFonts { Ascii = fontFamily, HighAnsi = fontFamily }
                    )
                )
            )
        );
        stylesPart.Styles.Save();
    }

    private static void AddSender(Body body, PersonalInfo info, string accentColor)
    {
        if (!string.IsNullOrWhiteSpace(info.FullName))
        {
            body.AppendChild(new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { After = "0" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize { Val = "36" }, new WColor { Val = accentColor }),
                    new Text(info.FullName)
                )
            ));
        }

        if (!string.IsNullOrWhiteSpace(info.JobTitle))
        {
            AddParagraph(body, info.JobTitle, spacingAfter: "0");
        }

        var contacts = new[] { info.Email, info.Phone, info.Location, info.Website }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        if (contacts.Count > 0)
        {
            body.AppendChild(new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { After = "200" }),
                new Run(
                    new RunProperties(new FontSize { Val = "18" }, new WColor { Val = "666666" }),
                    new Text(string.Join("  |  ", contacts))
                )
            ));
        }

        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Size = 6, Color = accentColor }
                ),
                new SpacingBetweenLines { After = "200" }
            )
        ));
    }

    private static void AddParagraph(
        Body body,
        string text,
        string spacingAfter = "100",
        string? spacingBefore = null,
        bool bold = false)
    {
        var spacing = new SpacingBetweenLines { After = spacingAfter };
        if (spacingBefore != null)
            spacing.Before = spacingBefore;

        var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        if (bold)
            run.PrependChild(new RunProperties(new Bold()));

        body.AppendChild(new Paragraph(new ParagraphProperties(spacing), run));
    }
}
