using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Templates;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using SectionType = ResumeBuilder.Core.Models.SectionType;
using WColor = DocumentFormat.OpenXml.Wordprocessing.Color;

namespace ResumeBuilder.Export;

public class DocxExporter : IExporter
{
    private readonly TemplateRegistry _templateRegistry;

    public string Format => "DOCX";
    public string FileExtension => ".docx";
    public string MimeType => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    // Word measures photos in EMUs: 914400 per inch.
    private const long PhotoSizeEmu = 914400L * 5 / 4;

    public DocxExporter(TemplateRegistry templateRegistry)
    {
        _templateRegistry = templateRegistry;
    }

    public async Task<byte[]> ExportAsync(Resume resume, string templateId)
    {
        var settings = SectionLayout.EffectiveSettings(resume, _templateRegistry, templateId);
        var accentColor = NormalizeColor(settings.AccentColor, TemplateSettings.DefaultAccentColor);
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

            foreach (var section in SectionLayout.OrderedVisibleSections(resume))
            {
                switch (section)
                {
                    case SectionType.PersonalInfo:
                        AddHeader(mainPart, body, resume.PersonalInfo, accentColor);
                        break;

                    case SectionType.Summary:
                        AddSectionTitle(body, "Professional Summary", accentColor);
                        AddParagraph(body, resume.Summary);
                        break;

                    case SectionType.Experience:
                        AddSectionTitle(body, "Work Experience", accentColor);
                        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            AddExperience(body, exp);
                        break;

                    case SectionType.Education:
                        AddSectionTitle(body, "Education", accentColor);
                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            AddEducation(body, edu);
                        break;

                    case SectionType.Skills:
                        AddSectionTitle(body, "Skills", accentColor);
                        AddSkills(body, resume.Skills);
                        break;

                    case SectionType.Languages:
                        AddSectionTitle(body, "Languages", accentColor);
                        foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                            AddParagraph(body, $"{lang.Name} - {SectionLayout.LanguageProficiencyText(lang.Proficiency)}");
                        break;

                    case SectionType.Certifications:
                        AddSectionTitle(body, "Certifications", accentColor);
                        foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                            AddCertification(body, cert);
                        break;

                    case SectionType.Projects:
                        AddSectionTitle(body, "Projects", accentColor);
                        foreach (var proj in resume.Projects.OrderBy(p => p.Order))
                            AddProject(body, proj);
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in SectionLayout.VisibleCustomSections(resume))
                        {
                            AddSectionTitle(body, custom.Title, accentColor);
                            foreach (var item in custom.Items.OrderBy(i => i.Order))
                                AddCustomItem(body, item);
                        }
                        break;
                }
            }
        }

        return await Task.FromResult(stream.ToArray());
    }

    public async Task ExportToFileAsync(Resume resume, string templateId, string filePath)
    {
        var bytes = await ExportAsync(resume, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
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

    private static void AddHeader(MainDocumentPart mainPart, Body body, PersonalInfo info, string accentColor)
    {
        if (info.Photo is { Length: > 0 })
        {
            AddPhoto(mainPart, body, info.Photo);
        }

        // Name
        var namePara = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { After = "0" }
            ),
            new Run(
                new RunProperties(
                    new Bold(),
                    new FontSize { Val = "48" },
                    new WColor { Val = accentColor }
                ),
                new Text(info.FullName)
            )
        );
        body.AppendChild(namePara);

        // Job title
        if (!string.IsNullOrWhiteSpace(info.JobTitle))
        {
            var titlePara = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "100" }
                ),
                new Run(
                    new RunProperties(new FontSize { Val = "24" }),
                    new Text(info.JobTitle)
                )
            );
            body.AppendChild(titlePara);
        }

        // Contact info
        var contacts = new List<string>();
        if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
        if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
        if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);
        if (!string.IsNullOrWhiteSpace(info.LinkedIn)) contacts.Add(info.LinkedIn);
        if (!string.IsNullOrWhiteSpace(info.GitHub)) contacts.Add(info.GitHub);
        if (!string.IsNullOrWhiteSpace(info.Website)) contacts.Add(info.Website);

        if (contacts.Any())
        {
            var contactPara = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "200" }
                ),
                new Run(
                    new RunProperties(new FontSize { Val = "20" }),
                    new Text(string.Join("  |  ", contacts))
                )
            );
            body.AppendChild(contactPara);
        }

        // Horizontal line
        var hrPara = new Paragraph(
            new ParagraphProperties(
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Size = 6, Color = "000000" }
                ),
                new SpacingBetweenLines { After = "200" }
            )
        );
        body.AppendChild(hrPara);
    }

    private static void AddPhoto(MainDocumentPart mainPart, Body body, byte[] photo)
    {
        try
        {
            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            using (var photoStream = new MemoryStream(photo))
            {
                imagePart.FeedData(photoStream);
            }

            var relationshipId = mainPart.GetIdOfPart(imagePart);

            var drawing = new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = PhotoSizeEmu, Cy = PhotoSizeEmu },
                    new DW.DocProperties { Id = 1U, Name = "Photo" },
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = 0U, Name = "Photo.png" },
                                    new PIC.NonVisualPictureDrawingProperties()
                                ),
                                new PIC.BlipFill(
                                    new A.Blip { Embed = relationshipId },
                                    new A.Stretch(new A.FillRectangle())
                                ),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = PhotoSizeEmu, Cy = PhotoSizeEmu }
                                    ),
                                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                                )
                            )
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                    )
                )
                { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U }
            );

            body.AppendChild(new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "100" }
                ),
                new Run(drawing)
            ));
        }
        catch
        {
            // An unreadable photo must not fail the whole export; the rest of the resume still exports.
        }
    }

    private static void AddSectionTitle(Body body, string title, string accentColor)
    {
        var para = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "200", After = "100" },
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = accentColor }
                )
            ),
            new Run(
                new RunProperties(
                    new Bold(),
                    new FontSize { Val = "24" },
                    new WColor { Val = accentColor }
                ),
                new Text(title.ToUpper())
            )
        );
        body.AppendChild(para);
    }

    private static void AddParagraph(Body body, string text, bool isBold = false)
    {
        var run = new Run(new Text(text));
        if (isBold) run.PrependChild(new RunProperties(new Bold()));

        var para = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "100" }),
            run
        );
        body.AppendChild(para);
    }

    private static void AddBullet(Body body, string text)
    {
        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new Indentation { Left = "360" },
                new SpacingBetweenLines { After = "50" }
            ),
            new Run(new Text($"• {text}"))
        ));
    }

    private static void AddExperience(Body body, Experience exp)
    {
        var titlePara = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "0" }),
            new Run(
                new RunProperties(new Bold(), new FontSize { Val = "22" }),
                new Text(exp.JobTitle)
            ),
            new Run(new Text("  |  ")),
            new Run(
                new RunProperties(new Italic()),
                new Text(exp.DateRange)
            )
        );
        body.AppendChild(titlePara);

        var companyPara = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "50" }),
            new Run(
                new RunProperties(new WColor { Val = "666666" }),
                new Text(exp.Company + (string.IsNullOrWhiteSpace(exp.Location) ? "" : $", {exp.Location}"))
            )
        );
        body.AppendChild(companyPara);

        if (!string.IsNullOrWhiteSpace(exp.Description))
        {
            AddParagraph(body, exp.Description);
        }

        foreach (var ach in exp.Achievements)
        {
            AddBullet(body, ach);
        }

        body.AppendChild(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "150" })));
    }

    private static void AddEducation(Body body, Education edu)
    {
        var titlePara = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "0" }),
            new Run(
                new RunProperties(new Bold(), new FontSize { Val = "22" }),
                new Text(edu.DegreeWithField)
            ),
            new Run(new Text("  |  ")),
            new Run(
                new RunProperties(new Italic()),
                new Text(edu.DateRange)
            )
        );
        body.AppendChild(titlePara);

        var instPara = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "100" }),
            new Run(
                new RunProperties(new WColor { Val = "666666" }),
                new Text(edu.Institution + (string.IsNullOrWhiteSpace(edu.Location) ? "" : $", {edu.Location}"))
            )
        );
        body.AppendChild(instPara);

        if (!string.IsNullOrWhiteSpace(edu.Grade))
        {
            AddParagraph(body, $"Grade: {edu.Grade}");
        }

        if (!string.IsNullOrWhiteSpace(edu.Description))
        {
            AddParagraph(body, edu.Description);
        }
    }

    private static void AddSkills(Body body, List<Skill> skills)
    {
        foreach (var group in skills.GroupBy(s => s.Category))
        {
            var entries = group.OrderBy(s => s.Order)
                .Select(s => $"{s.Name} ({SectionLayout.SkillLevelText(s.Level)})");

            var text = string.Join(", ", entries);

            if (string.IsNullOrWhiteSpace(group.Key))
            {
                AddParagraph(body, text);
                continue;
            }

            body.AppendChild(new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { After = "100" }),
                new Run(new RunProperties(new Bold()), new Text($"{group.Key}: ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new Text(text))
            ));
        }
    }

    private static void AddCertification(Body body, Certification cert)
    {
        var certText = cert.Name;
        if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
            certText += $" - {cert.IssuingOrganization}";

        var date = SectionLayout.CertificationDate(cert);
        if (!string.IsNullOrEmpty(date))
            certText += $" ({date})";

        AddParagraph(body, certText);

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(cert.CredentialId))
            details.Add($"Credential ID: {cert.CredentialId}");
        if (!string.IsNullOrWhiteSpace(cert.CredentialUrl))
            details.Add(cert.CredentialUrl);

        if (details.Any())
        {
            body.AppendChild(new Paragraph(
                new ParagraphProperties(
                    new Indentation { Left = "360" },
                    new SpacingBetweenLines { After = "100" }
                ),
                new Run(
                    new RunProperties(new WColor { Val = "666666" }),
                    new Text(string.Join("  |  ", details))
                )
            ));
        }
    }

    private static void AddProject(Body body, Project proj)
    {
        var titlePara = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "50" }),
            new Run(
                new RunProperties(new Bold()),
                new Text(proj.Name)
            )
        );

        var dateRange = FormatDateRange(proj.StartDate, proj.EndDate, proj.IsOngoing);
        if (!string.IsNullOrEmpty(dateRange))
        {
            titlePara.AppendChild(new Run(new Text("  |  ")));
            titlePara.AppendChild(new Run(new RunProperties(new Italic()), new Text(dateRange)));
        }

        body.AppendChild(titlePara);

        if (!string.IsNullOrWhiteSpace(proj.Url))
        {
            body.AppendChild(new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { After = "50" }),
                new Run(
                    new RunProperties(new WColor { Val = "666666" }),
                    new Text(proj.Url)
                )
            ));
        }

        if (!string.IsNullOrWhiteSpace(proj.Description))
        {
            AddParagraph(body, proj.Description);
        }

        foreach (var highlight in proj.Highlights)
        {
            AddBullet(body, highlight);
        }

        if (proj.Technologies.Any())
        {
            AddParagraph(body, $"Technologies: {string.Join(", ", proj.Technologies)}");
        }
    }

    private static void AddCustomItem(Body body, CustomSectionItem item)
    {
        var titlePara = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "0" }),
            new Run(
                new RunProperties(new Bold(), new FontSize { Val = "22" }),
                new Text(item.Title)
            )
        );

        var dateRange = SectionLayout.CustomItemDateRange(item);
        if (!string.IsNullOrEmpty(dateRange))
        {
            titlePara.AppendChild(new Run(new Text("  |  ")));
            titlePara.AppendChild(new Run(new RunProperties(new Italic()), new Text(dateRange)));
        }

        body.AppendChild(titlePara);

        if (!string.IsNullOrWhiteSpace(item.Subtitle))
        {
            body.AppendChild(new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { After = "50" }),
                new Run(
                    new RunProperties(new WColor { Val = "666666" }),
                    new Text(item.Subtitle)
                )
            ));
        }

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            AddParagraph(body, item.Description);
        }
    }

    private static string FormatDateRange(DateTime? start, DateTime? end, bool isOngoing)
    {
        if (!start.HasValue)
            return string.Empty;

        var endStr = isOngoing ? "Present" : ResumeDateFormat.MonthYear(end);
        return $"{ResumeDateFormat.MonthYear(start)} - {endStr}";
    }

    /// <summary>Word wants a bare 6-digit RRGGBB value; anything else falls back to the default.</summary>
    internal static string NormalizeColor(string? color, string fallback)
    {
        var value = (color ?? "").TrimStart('#');

        if (value.Length == 8)
            value = value[2..]; // drop the alpha channel Word cannot represent

        if (value.Length == 3)
            value = string.Concat(value.Select(c => new string(c, 2)));

        return value.Length == 6 && value.All(Uri.IsHexDigit)
            ? value.ToUpperInvariant()
            : fallback.TrimStart('#').ToUpperInvariant();
    }
}
