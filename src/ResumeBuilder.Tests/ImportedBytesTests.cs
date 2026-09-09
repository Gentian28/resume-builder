using AwesomeAssertions;
using ResumeBuilder.Export;
using ResumeBuilder.Export.Importers;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Tests;

/// <summary>
/// Bytes that arrive from a file are not trusted by the renderers: a photo that is not an image
/// must not take down every render, and a control character must not produce a DOCX Word refuses.
/// </summary>
public class ImportedBytesTests
{
    private static readonly byte[] NotAnImage = System.Text.Encoding.UTF8.GetBytes("definitely not a picture");

    [Fact]
    public void PhotoBytes_RecognisesARealImageAndRejectsGarbage()
    {
        PhotoBytes.IsRenderable(TestResumes.SamplePhoto()).Should().BeTrue();
        PhotoBytes.IsRenderable(NotAnImage).Should().BeFalse();
        PhotoBytes.IsRenderable(null).Should().BeFalse();
        PhotoBytes.IsRenderable(Array.Empty<byte>()).Should().BeFalse();
    }

    [Theory]
    [InlineData("photo-header")]
    [InlineData("europass")]
    [InlineData("modern")]
    public async Task Template_WithAPhotoThatIsNotAnImage_StillRenders(string templateId)
    {
        var service = new ExportService(new TemplateRegistry());
        var resume = TestResumes.FullyPopulated();
        resume.SelectedTemplateId = templateId;
        resume.PersonalInfo.Photo = NotAnImage;

        var pdf = await service.ExportAsync(resume, "PDF");

        pdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task JsonImport_DropsAPhotoThatIsNotAnImage_AndSaysSo()
    {
        var resume = TestResumes.FullyPopulated();
        resume.PersonalInfo.Photo = NotAnImage;
        var json = await new JsonExporter().ExportAsync(resume, resume.SelectedTemplateId);

        var result = await new JsonImporter().ImportAsync(new MemoryStream(json));

        result.Success.Should().BeTrue();
        result.Data!.PersonalInfo.Photo.Should().BeNull();
        result.Warnings.Should().ContainSingle().Which.Should().Contain("photo");
    }

    [Fact]
    public async Task JsonImport_KeepsAPhotoThatIsAnImage()
    {
        var resume = TestResumes.FullyPopulated();
        resume.PersonalInfo.Photo = TestResumes.SamplePhoto();
        var json = await new JsonExporter().ExportAsync(resume, resume.SelectedTemplateId);

        var result = await new JsonImporter().ImportAsync(new MemoryStream(json));

        result.Data!.PersonalInfo.Photo.Should().Equal(TestResumes.SamplePhoto());
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void XmlText_RemovesWhatXmlCannotCarry_AndKeepsTheRest()
    {
        XmlText.Clean("a\u0001b\u000Cc").Should().Be("abc");
        XmlText.Clean("tab\tnew\nline\r").Should().Be("tab\tnew\nline\r");
        XmlText.Clean("emoji \U0001F600 kept").Should().Be("emoji \U0001F600 kept");
        XmlText.Clean("lone \uD83D surrogate").Should().Be("lone  surrogate");
        XmlText.Clean(null).Should().BeEmpty();
    }

    [Fact]
    public async Task DocxExport_WithControlCharactersInTheText_ProducesAFileThatOpens()
    {
        var resume = TestResumes.FullyPopulated();
        resume.PersonalInfo.FirstName = "Ja\u000Cne";
        resume.Experiences[0].Company = "Acme\u000CCorp";
        resume.Summary = "Pasted from a PDF\u0001with a stray control character";

        var bytes = await new ExportService(new TemplateRegistry()).ExportAsync(resume, "DOCX");

        bytes.Should().NotBeNullOrEmpty();
        using var stream = new MemoryStream(bytes!);
        using var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document.Body!.InnerText;
        text.Should().Contain("Jane");
        text.Should().Contain("AcmeCorp");
        text.Should().Contain("Pasted from a PDFwith a stray control character");
    }
}
