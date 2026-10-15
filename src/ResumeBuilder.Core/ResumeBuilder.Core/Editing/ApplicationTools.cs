using System.Globalization;
using System.Text;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Core.Editing;

public static class ApplicationTools
{
    public static bool IsArchived(ApplicationStatus status) => status is ApplicationStatus.Offer or ApplicationStatus.Rejected or ApplicationStatus.NoResponse;
    public static bool IsDue(JobApplication row, DateTime today) => row.NextActionOn?.Date <= today.Date && !IsArchived(row.Status);

    public static string Csv(IEnumerable<JobApplication> rows)
    {
        static string Cell(string? value)
        {
            value ??= "";
            var trimmed = value.TrimStart();
            if (trimmed.Length > 0 && ("=+-@".Contains(trimmed[0]) || value[0] is '\t' or '\r' or '\n')) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        var output = new StringBuilder("Company,Role,Status,Applied date,Next action,Contact,Posting URL,Notes\r\n");
        foreach (var r in rows)
            output.AppendLine(string.Join(",", new[] { r.Company, r.Role, r.Status.ToString(), r.AppliedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.NextActionOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), r.Contact, r.Link, r.Notes }.Select(Cell)));
        return output.ToString();
    }

    public static string Calendar(IEnumerable<JobApplication> rows)
    {
        static string Escape(string s) => s.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\\n");
        var lines = new List<string> { "BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//ResumeBuilder//Follow-ups//EN", "CALSCALE:GREGORIAN" };
        foreach (var r in rows.Where(r => r.NextActionOn != null && !IsArchived(r.Status)))
        {
            lines.AddRange(new[] { "BEGIN:VEVENT", $"UID:application-{r.Id}-{r.CreatedAt.Ticks}@resumebuilder.local", $"DTSTAMP:{DateTime.SpecifyKind(r.UpdatedAt, DateTimeKind.Utc).ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture)}",
                $"DTSTART;VALUE=DATE:{r.NextActionOn!.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}", $"DTEND;VALUE=DATE:{r.NextActionOn.Value.AddDays(1).ToString("yyyyMMdd", CultureInfo.InvariantCulture)}",
                "SUMMARY:" + Escape($"Follow up: {r.Company} - {r.Role}"), "DESCRIPTION:" + Escape(string.Join("\n", r.Contact, r.Link, r.Notes)), "END:VEVENT" });
        }
        lines.Add("END:VCALENDAR");
        // RFC 5545 folds at 75 UTF-8 octets without splitting a Unicode scalar.
        var output = new StringBuilder();
        foreach (var line in lines)
        {
            var octets = 0;
            foreach (var rune in line.EnumerateRunes())
            {
                if (octets + rune.Utf8SequenceLength > 75) { output.Append("\r\n "); octets = 1; }
                output.Append(rune.ToString()); octets += rune.Utf8SequenceLength;
            }
            output.Append("\r\n");
        }
        return output.ToString();
    }
}
