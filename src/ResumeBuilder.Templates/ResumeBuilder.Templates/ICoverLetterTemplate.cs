using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates;

public interface ICoverLetterTemplate
{
    TemplateInfo Info { get; }
    IDocument CreateDocument(CoverLetter letter);
}
