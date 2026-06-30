using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates;

public interface IResumeTemplate
{
    TemplateInfo Info { get; }
    IDocument CreateDocument(Resume resume);
}
