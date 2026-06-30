using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Export;

public class DocxExporter : IExporter
{
    private readonly TemplateRegistry _templateRegistry;

    public string Format => "DOCX";
    public string FileExtension => ".docx";
    public string MimeType => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public DocxExporter(TemplateRegistry templateRegistry)
    {
        _templateRegistry = templateRegistry;
    }

    public async Task<byte[]> ExportAsync(Resume resume, string templateId)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Add resume content
            AddHeader(body, resume.PersonalInfo, resume.AccentColor);

            if (!string.IsNullOrWhiteSpace(resume.Summary))
            {
                AddSectionTitle(body, "Professional Summary", resume.AccentColor);
                AddParagraph(body, resume.Summary);
            }

            if (resume.Experiences.Any())
            {
                AddSectionTitle(body, "Work Experience", resume.AccentColor);
                foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                {
                    AddExperience(body, exp);
                }
            }

            if (resume.EducationList.Any())
            {
                AddSectionTitle(body, "Education", resume.AccentColor);
                foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                {
                    AddEducation(body, edu);
                }
            }

            if (resume.Skills.Any())
            {
                AddSectionTitle(body, "Skills", resume.AccentColor);
                var skillText = string.Join(", ", resume.Skills.OrderBy(s => s.Order).Select(s => s.Name));
                AddParagraph(body, skillText);
            }

            if (resume.Languages.Any())
            {
                AddSectionTitle(body, "Languages", resume.AccentColor);
                foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                {
                    AddParagraph(body, $"{lang.Name} - {GetLanguageProficiencyText(lang.Proficiency)}");
                }
            }

            if (resume.Certifications.Any())
            {
                AddSectionTitle(body, "Certifications", resume.AccentColor);
                foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                {
                    var certText = cert.Name;
                    if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                        certText += $" - {cert.IssuingOrganization}";
                    if (cert.IssueDate.HasValue)
                        certText += $" ({cert.IssueDate.Value:MMM yyyy})";
                    AddParagraph(body, certText);
                }
            }

            if (resume.Projects.Any())
            {
                AddSectionTitle(body, "Projects", resume.AccentColor);
                foreach (var proj in resume.Projects.OrderBy(p => p.Order))
                {
                    AddProject(body, proj);
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

    private static void AddHeader(Body body, PersonalInfo info, string accentColor)
    {
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
                    new Color { Val = accentColor.Replace("#", "") }
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

    private static void AddSectionTitle(Body body, string title, string accentColor)
    {
        var para = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "200", After = "100" },
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = accentColor.Replace("#", "") }
                )
            ),
            new Run(
                new RunProperties(
                    new Bold(),
                    new FontSize { Val = "24" },
                    new Color { Val = accentColor.Replace("#", "") }
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

    private static void AddExperience(Body body, Experience exp)
    {
        // Title and dates
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

        // Company
        var companyPara = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "50" }),
            new Run(
                new RunProperties(new Color { Val = "666666" }),
                new Text(exp.Company + (string.IsNullOrWhiteSpace(exp.Location) ? "" : $", {exp.Location}"))
            )
        );
        body.AppendChild(companyPara);

        // Description
        if (!string.IsNullOrWhiteSpace(exp.Description))
        {
            AddParagraph(body, exp.Description);
        }

        // Achievements
        foreach (var ach in exp.Achievements)
        {
            var achPara = new Paragraph(
                new ParagraphProperties(
                    new Indentation { Left = "360" },
                    new SpacingBetweenLines { After = "50" }
                ),
                new Run(new Text($"• {ach}"))
            );
            body.AppendChild(achPara);
        }

        // Spacing after experience
        body.AppendChild(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "150" })));
    }

    private static void AddEducation(Body body, Education edu)
    {
        // Degree and dates
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

        // Institution
        var instPara = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "100" }),
            new Run(
                new RunProperties(new Color { Val = "666666" }),
                new Text(edu.Institution + (string.IsNullOrWhiteSpace(edu.Location) ? "" : $", {edu.Location}"))
            )
        );
        body.AppendChild(instPara);

        if (!string.IsNullOrWhiteSpace(edu.Grade))
        {
            AddParagraph(body, $"Grade: {edu.Grade}");
        }
    }

    private static void AddProject(Body body, Project proj)
    {
        // Project name
        var titlePara = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "50" }),
            new Run(
                new RunProperties(new Bold()),
                new Text(proj.Name)
            )
        );
        body.AppendChild(titlePara);

        if (!string.IsNullOrWhiteSpace(proj.Description))
        {
            AddParagraph(body, proj.Description);
        }

        if (proj.Technologies.Any())
        {
            AddParagraph(body, $"Technologies: {string.Join(", ", proj.Technologies)}");
        }
    }

    private static string GetLanguageProficiencyText(LanguageProficiency level) => level switch
    {
        LanguageProficiency.Basic => "Basic",
        LanguageProficiency.Conversational => "Conversational",
        LanguageProficiency.Professional => "Professional",
        LanguageProficiency.Fluent => "Fluent",
        LanguageProficiency.Native => "Native",
        _ => "Professional"
    };
}
