namespace ResumeBuilder.Core.Models;

public sealed record SentDocumentArtifacts(byte[]? ResumePdf, byte[]? CoverLetterPdf);

public interface ISentDocumentRenderer
{
    Task<SentDocumentArtifacts> CaptureAsync(Resume? resume, CoverLetter? letter);
}
