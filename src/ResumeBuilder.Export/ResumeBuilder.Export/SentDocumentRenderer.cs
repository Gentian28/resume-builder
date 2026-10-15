using ResumeBuilder.Core.Models;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Export;

public sealed class SentDocumentRenderer(TemplateRegistry registry) : ISentDocumentRenderer
{
    public Task<SentDocumentArtifacts> CaptureAsync(Resume? resume, CoverLetter? letter) => Task.Run(async () =>
        new SentDocumentArtifacts(resume is null ? null : await new PdfExporter(registry).ExportAsync(resume, resume.SelectedTemplateId),
            letter is null ? null : await new CoverLetterPdfExporter(registry).ExportAsync(letter, letter.SelectedTemplateId)));
}
