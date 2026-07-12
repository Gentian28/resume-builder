using System.Text;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export;

/// <summary>Plain-text cover letter, for application forms that only accept pasted text.</summary>
public class CoverLetterTextExporter : ICoverLetterExporter
{
    public string Format => "TEXT";
    public string FileExtension => ".txt";
    public string MimeType => "text/plain";

    public Task<byte[]> ExportAsync(CoverLetter letter, string templateId)
    {
        var sb = new StringBuilder();
        var info = letter.PersonalInfo;

        AppendIfPresent(sb, info.FullName);
        AppendIfPresent(sb, info.JobTitle);

        var contacts = new[] { info.Email, info.Phone, info.Location, info.Website }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        if (contacts.Count > 0)
            sb.AppendLine(string.Join("  |  ", contacts));

        sb.AppendLine();
        sb.AppendLine(letter.LetterDate.ToString("d MMMM yyyy", ResumeDateFormat.Culture));
        sb.AppendLine();

        AppendIfPresent(sb, letter.RecipientName);
        AppendIfPresent(sb, letter.RecipientTitle);
        AppendIfPresent(sb, letter.CompanyName);

        foreach (var line in letter.CompanyAddress.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            AppendIfPresent(sb, line);
        }

        if (!string.IsNullOrWhiteSpace(letter.Subject))
        {
            sb.AppendLine();
            sb.AppendLine(letter.Subject.Trim());
        }

        sb.AppendLine();
        sb.AppendLine(letter.EffectiveSalutation);

        foreach (var paragraph in letter.Paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            sb.AppendLine();
            sb.AppendLine(paragraph.Trim());
        }

        sb.AppendLine();
        AppendIfPresent(sb, letter.Closing);
        sb.AppendLine();
        AppendIfPresent(sb, letter.SignatureName);

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString().TrimEnd() + Environment.NewLine));
    }

    public async Task ExportToFileAsync(CoverLetter letter, string templateId, string filePath)
    {
        var bytes = await ExportAsync(letter, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    private static void AppendIfPresent(StringBuilder sb, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            sb.AppendLine(value.Trim());
    }
}
