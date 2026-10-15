using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export.Importers;

public sealed class DocxImporter : IImporter
{
    public string Format => "DOCX";
    public string[] SupportedExtensions => [".docx"];
    public async Task<ImportResult<Resume>> ImportAsync(Stream stream)
    {
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            ImportLimits.ValidateArchive(archive);
            var document = archive.GetEntry("word/document.xml") ?? throw new InvalidDataException("This file has no Word document body.");
            using var input = document.Open();
            var bytes = await ImportLimits.ReadAsync(input, 16 * 1024 * 1024);
            using var xmlInput = new MemoryStream(bytes);
            using var reader = XmlReader.Create(xmlInput, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 16 * 1024 * 1024 });
            var xml = XDocument.Load(reader);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var paragraphs = xml.Descendants(w + "p").Where(p => !p.Ancestors(w + "del").Any())
                .Select(p => (
                    Text: string.Concat(p.Descendants().Where(n => !n.Ancestors(w + "del").Any() && (n.Name == w + "t" || n.Name == w + "tab" || n.Name == w + "br"))
                        .Select(n => n.Name == w + "t" ? n.Value : n.Name == w + "tab" ? " " : "\n")),
                    FontSize: p.Descendants(w + "sz").Where(n => !n.Ancestors(w + "del").Any()).Select(n => int.TryParse((string?)n.Attribute(w + "val"), out var size) ? size / 2d : 12).DefaultIfEmpty(12).Max(),
                    IsBold: p.Descendants(w + "b").Any(n => !n.Ancestors(w + "del").Any() && (string?)n.Attribute(w + "val") is not ("0" or "false" or "off"))))
                .Where(p => !string.IsNullOrWhiteSpace(p.Text)).ToList();
            var lines = paragraphs.Select(p => p.Text).ToList();
            if (lines.Count == 0) return ImportResult<Resume>.Failed("No readable body text was found in this Word document.");
            var result = new PdfImporter().ImportText(paragraphs);
            result.Data!.Name = string.IsNullOrWhiteSpace(result.Data.PersonalInfo.FullName) ? "Imported Word resume" : $"{result.Data.PersonalInfo.FullName} (Word import)";
            result.Data.CustomSections.Add(new CustomSection { Title = "Original Word content", Items = lines.Select((s, i) => new CustomSectionItem { Order = i, Description = s }).ToList() });
            result.Warnings.Add("Word formatting and field detection are approximate. All body text is preserved in Original Word content; review it before removing duplicates. Images, headers, footers, comments and deleted revisions are not imported.");
            return result;
        }
        catch (Exception ex) { return ImportResult<Resume>.Failed($"Could not import Word document: {ex.Message}"); }
    }
    public async Task<ImportResult<Resume>> ImportFromFileAsync(string path)
    {
        using var file = File.OpenRead(path);
        return await ImportAsync(file);
    }
}
