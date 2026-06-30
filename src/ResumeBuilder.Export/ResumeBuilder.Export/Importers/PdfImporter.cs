using System.Text;
using System.Text.RegularExpressions;
using ResumeBuilder.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ResumeBuilder.Export.Importers;

public class PdfImporter : IImporter
{
    public string Format => "PDF";
    public string[] SupportedExtensions => new[] { ".pdf" };

    // Section header patterns
    private static readonly string[] ExperienceHeaders = { "experience", "work experience", "employment", "work history", "professional experience", "career history" };
    private static readonly string[] EducationHeaders = { "education", "academic", "qualifications", "academic background" };
    private static readonly string[] SkillsHeaders = { "skills", "technical skills", "core competencies", "competencies", "expertise", "abilities" };
    private static readonly string[] SummaryHeaders = { "summary", "profile", "professional summary", "about", "objective", "career objective", "professional profile" };
    private static readonly string[] CertificationHeaders = { "certifications", "certificates", "licenses", "credentials" };
    private static readonly string[] ProjectHeaders = { "projects", "key projects", "personal projects" };
    private static readonly string[] LanguageHeaders = { "languages", "language skills" };

    public async Task<ImportResult<Resume>> ImportAsync(Stream stream)
    {
        try
        {
            // Copy stream to memory for PdfPig (it needs seekable stream)
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var document = PdfDocument.Open(memoryStream);
            var warnings = new List<string>();
            var resume = new Resume();

            // Extract all text with positions
            var allText = new StringBuilder();
            var textBlocks = new List<TextBlock>();

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                var pageText = string.Join(" ", words.Select(w => w.Text));
                allText.AppendLine(pageText);

                // Group words into lines based on Y position
                var lines = GroupWordsIntoLines(words);
                textBlocks.AddRange(lines);
            }

            var fullText = allText.ToString();

            // Try to extract contact information from the first section
            ExtractContactInfo(textBlocks.Take(20).ToList(), resume, warnings);

            // Parse sections
            var sections = DetectSections(textBlocks);

            // Process each section
            foreach (var section in sections)
            {
                var sectionText = string.Join("\n", section.Lines.Select(l => l.Text));

                if (MatchesHeader(section.Header, SummaryHeaders))
                {
                    resume.Summary = CleanText(sectionText);
                }
                else if (MatchesHeader(section.Header, ExperienceHeaders))
                {
                    resume.Experiences = ParseExperiences(section.Lines);
                }
                else if (MatchesHeader(section.Header, EducationHeaders))
                {
                    resume.EducationList = ParseEducation(section.Lines);
                }
                else if (MatchesHeader(section.Header, SkillsHeaders))
                {
                    resume.Skills = ParseSkills(sectionText);
                }
                else if (MatchesHeader(section.Header, CertificationHeaders))
                {
                    resume.Certifications = ParseCertifications(section.Lines);
                }
                else if (MatchesHeader(section.Header, ProjectHeaders))
                {
                    resume.Projects = ParseProjects(section.Lines);
                }
                else if (MatchesHeader(section.Header, LanguageHeaders))
                {
                    resume.Languages = ParseLanguages(sectionText);
                }
            }

            // Set resume name
            if (!string.IsNullOrEmpty(resume.PersonalInfo.FullName))
                resume.Name = $"{resume.PersonalInfo.FullName}'s Resume (PDF Import)";
            else
                resume.Name = "Imported Resume (PDF)";

            // Add warning if little data was extracted
            if (string.IsNullOrEmpty(resume.PersonalInfo.FirstName) &&
                string.IsNullOrEmpty(resume.Summary) &&
                !resume.Experiences.Any())
            {
                warnings.Add("Limited data could be extracted. PDF parsing works best with text-based resumes. Scanned documents may require manual entry.");
            }

            return ImportResult<Resume>.Succeeded(resume, warnings);
        }
        catch (Exception ex)
        {
            return ImportResult<Resume>.Failed($"Error parsing PDF: {ex.Message}");
        }
    }

    public async Task<ImportResult<Resume>> ImportFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return ImportResult<Resume>.Failed($"File not found: {filePath}");

        await using var stream = File.OpenRead(filePath);
        return await ImportAsync(stream);
    }

    private List<TextBlock> GroupWordsIntoLines(IEnumerable<Word> words)
    {
        var lines = new List<TextBlock>();
        var wordList = words.OrderByDescending(w => w.BoundingBox.Top).ThenBy(w => w.BoundingBox.Left).ToList();

        TextBlock? currentLine = null;
        double lastY = double.MaxValue;
        const double lineThreshold = 5; // pixels tolerance for same line

        foreach (var word in wordList)
        {
            var y = word.BoundingBox.Top;

            if (currentLine == null || Math.Abs(y - lastY) > lineThreshold)
            {
                if (currentLine != null)
                    lines.Add(currentLine);

                currentLine = new TextBlock
                {
                    Text = word.Text,
                    Y = y,
                    FontSize = word.Letters.FirstOrDefault()?.PointSize ?? 12,
                    IsBold = IsBoldFont(word)
                };
            }
            else
            {
                currentLine.Text += " " + word.Text;
            }

            lastY = y;
        }

        if (currentLine != null)
            lines.Add(currentLine);

        return lines;
    }

    private bool IsBoldFont(Word word)
    {
        var fontName = word.Letters.FirstOrDefault()?.FontName?.ToLower() ?? "";
        return fontName.Contains("bold") || fontName.Contains("heavy") || fontName.Contains("black");
    }

    private void ExtractContactInfo(List<TextBlock> headerLines, Resume resume, List<string> warnings)
    {
        var text = string.Join(" ", headerLines.Select(l => l.Text));

        // Extract email
        var emailMatch = Regex.Match(text, @"[\w\.-]+@[\w\.-]+\.\w+");
        if (emailMatch.Success)
            resume.PersonalInfo.Email = emailMatch.Value;

        // Extract phone
        var phoneMatch = Regex.Match(text, @"[\+]?[(]?[0-9]{1,3}[)]?[-\s\.]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,4}[-\s\.]?[0-9]{1,9}");
        if (phoneMatch.Success)
            resume.PersonalInfo.Phone = phoneMatch.Value;

        // Extract LinkedIn
        var linkedInMatch = Regex.Match(text, @"linkedin\.com/in/[\w-]+", RegexOptions.IgnoreCase);
        if (linkedInMatch.Success)
            resume.PersonalInfo.LinkedIn = "https://" + linkedInMatch.Value;

        // Extract GitHub
        var githubMatch = Regex.Match(text, @"github\.com/[\w-]+", RegexOptions.IgnoreCase);
        if (githubMatch.Success)
            resume.PersonalInfo.GitHub = "https://" + githubMatch.Value;

        // Extract website
        var urlMatch = Regex.Match(text, @"https?://[^\s]+|www\.[^\s]+");
        if (urlMatch.Success && !urlMatch.Value.Contains("linkedin") && !urlMatch.Value.Contains("github"))
            resume.PersonalInfo.Website = urlMatch.Value;

        // Try to extract name (usually the first large/bold text)
        var nameCandidate = headerLines.FirstOrDefault(l => l.FontSize > 14 || l.IsBold);
        if (nameCandidate != null)
        {
            var name = CleanText(nameCandidate.Text);
            // Filter out common non-name content
            if (!string.IsNullOrEmpty(name) &&
                !name.Contains("@") &&
                !name.Contains("http") &&
                !Regex.IsMatch(name, @"^\d") &&
                name.Length < 50)
            {
                var nameParts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length >= 1)
                    resume.PersonalInfo.FirstName = nameParts[0];
                if (nameParts.Length >= 2)
                    resume.PersonalInfo.LastName = string.Join(" ", nameParts.Skip(1));
            }
        }

        // Try to extract job title (usually second line, often italic or smaller than name)
        if (headerLines.Count > 1)
        {
            var titleCandidate = headerLines.Skip(1).FirstOrDefault(l =>
                !l.Text.Contains("@") &&
                !l.Text.Contains("http") &&
                !Regex.IsMatch(l.Text, @"^\d{3}") && // Not a phone number
                l.Text.Length < 100);

            if (titleCandidate != null)
            {
                var title = CleanText(titleCandidate.Text);
                if (!string.IsNullOrEmpty(title) && !title.Contains(resume.PersonalInfo.Email ?? "xxx"))
                    resume.PersonalInfo.JobTitle = title;
            }
        }
    }

    private List<ResumeSection> DetectSections(List<TextBlock> lines)
    {
        var sections = new List<ResumeSection>();
        ResumeSection? currentSection = null;

        var allHeaders = ExperienceHeaders.Concat(EducationHeaders)
            .Concat(SkillsHeaders).Concat(SummaryHeaders)
            .Concat(CertificationHeaders).Concat(ProjectHeaders)
            .Concat(LanguageHeaders).ToArray();

        foreach (var line in lines)
        {
            var lineText = line.Text.Trim().ToLower();
            var isHeader = allHeaders.Any(h => lineText.Equals(h) ||
                lineText.StartsWith(h + " ") ||
                lineText.EndsWith(" " + h));

            // Also detect headers by formatting (bold, larger font, all caps)
            var mightBeHeader = (line.IsBold || line.FontSize > 13 || line.Text == line.Text.ToUpper()) &&
                                line.Text.Length < 40 &&
                                !line.Text.Contains("@");

            if (isHeader || (mightBeHeader && allHeaders.Any(h => lineText.Contains(h))))
            {
                if (currentSection != null)
                    sections.Add(currentSection);

                currentSection = new ResumeSection
                {
                    Header = line.Text.Trim(),
                    Lines = new List<TextBlock>()
                };
            }
            else if (currentSection != null)
            {
                currentSection.Lines.Add(line);
            }
        }

        if (currentSection != null)
            sections.Add(currentSection);

        return sections;
    }

    private bool MatchesHeader(string header, string[] patterns)
    {
        var h = header.ToLower().Trim();
        return patterns.Any(p => h.Contains(p));
    }

    private List<Experience> ParseExperiences(List<TextBlock> lines)
    {
        var experiences = new List<Experience>();
        Experience? current = null;
        var descriptionBuilder = new StringBuilder();

        foreach (var line in lines)
        {
            var text = line.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            // Detect date patterns
            var dateMatch = Regex.Match(text, @"(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|January|February|March|April|May|June|July|August|September|October|November|December)[\s,]*\d{4}\s*[-–—to]+\s*(Present|Current|Now|(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|January|February|March|April|May|June|July|August|September|October|November|December)[\s,]*\d{4}|\d{4})", RegexOptions.IgnoreCase);

            var yearRangeMatch = Regex.Match(text, @"\b(19|20)\d{2}\s*[-–—to]+\s*(Present|Current|Now|(19|20)\d{2})\b", RegexOptions.IgnoreCase);

            if (dateMatch.Success || yearRangeMatch.Success)
            {
                // Save previous experience
                if (current != null)
                {
                    current.Description = CleanText(descriptionBuilder.ToString());
                    experiences.Add(current);
                    descriptionBuilder.Clear();
                }

                current = new Experience { Order = experiences.Count };
                var match = dateMatch.Success ? dateMatch : yearRangeMatch;

                // Parse dates
                var dateText = match.Value;
                var dateParts = Regex.Split(dateText, @"\s*[-–—to]+\s*", RegexOptions.IgnoreCase);
                if (dateParts.Length >= 1)
                    current.StartDate = ParseDate(dateParts[0]);
                if (dateParts.Length >= 2)
                {
                    if (Regex.IsMatch(dateParts[1], @"present|current|now", RegexOptions.IgnoreCase))
                        current.IsCurrentRole = true;
                    else
                        current.EndDate = ParseDate(dateParts[1]);
                }

                // Extract job title and company from the rest of the line
                var remainingText = text.Replace(match.Value, "").Trim(' ', '-', '–', '—', '|', ',');
                if (!string.IsNullOrEmpty(remainingText))
                {
                    var parts = Regex.Split(remainingText, @"\s*[|,@at]\s*", RegexOptions.IgnoreCase);
                    if (parts.Length >= 1)
                        current.JobTitle = parts[0].Trim();
                    if (parts.Length >= 2)
                        current.Company = parts[1].Trim();
                }
            }
            else if (current != null)
            {
                // Check if this might be a job title or company on its own line
                if (string.IsNullOrEmpty(current.JobTitle) && line.IsBold)
                {
                    current.JobTitle = text;
                }
                else if (string.IsNullOrEmpty(current.Company) && !text.StartsWith("•") && !text.StartsWith("-") && text.Length < 60)
                {
                    current.Company = text;
                }
                else
                {
                    // It's description/achievement
                    var cleanedText = Regex.Replace(text, @"^[\s•\-\*]+", "").Trim();
                    if (!string.IsNullOrEmpty(cleanedText))
                    {
                        if (text.StartsWith("•") || text.StartsWith("-") || text.StartsWith("*"))
                            current.Achievements.Add(cleanedText);
                        else
                            descriptionBuilder.AppendLine(cleanedText);
                    }
                }
            }
        }

        if (current != null)
        {
            current.Description = CleanText(descriptionBuilder.ToString());
            experiences.Add(current);
        }

        return experiences;
    }

    private List<Education> ParseEducation(List<TextBlock> lines)
    {
        var education = new List<Education>();
        Education? current = null;

        foreach (var line in lines)
        {
            var text = line.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            // Look for degree patterns
            var degreeMatch = Regex.Match(text, @"\b(Bachelor|Master|PhD|Ph\.D|MBA|BSc|BA|MS|MA|B\.S\.|M\.S\.|Associate|Diploma|Certificate)\b", RegexOptions.IgnoreCase);

            // Look for year patterns
            var yearMatch = Regex.Match(text, @"\b(19|20)\d{2}\b");

            if (degreeMatch.Success || (line.IsBold && yearMatch.Success))
            {
                if (current != null)
                    education.Add(current);

                current = new Education { Order = education.Count };

                if (degreeMatch.Success)
                {
                    var degreeText = text;
                    // Try to extract field of study
                    var inMatch = Regex.Match(text, @"in\s+(.+?)(?:\s*[-–|,]|$)", RegexOptions.IgnoreCase);
                    if (inMatch.Success)
                    {
                        current.FieldOfStudy = inMatch.Groups[1].Value.Trim();
                        current.Degree = text.Substring(0, inMatch.Index).Trim();
                    }
                    else
                    {
                        current.Degree = text;
                    }
                }
            }
            else if (current != null)
            {
                // Try to identify institution
                if (string.IsNullOrEmpty(current.Institution) &&
                    (text.Contains("University") || text.Contains("College") || text.Contains("Institute") || text.Contains("School")))
                {
                    current.Institution = text;
                }
                // Try to extract dates
                else if (yearMatch.Success && current.StartDate == null)
                {
                    var years = Regex.Matches(text, @"\b(19|20)\d{2}\b");
                    if (years.Count >= 1)
                        current.StartDate = new DateTime(int.Parse(years[0].Value), 1, 1);
                    if (years.Count >= 2)
                        current.EndDate = new DateTime(int.Parse(years[1].Value), 1, 1);
                    else if (years.Count == 1 && text.ToLower().Contains("present"))
                        current.IsCurrentlyStudying = true;
                }
                else if (string.IsNullOrEmpty(current.Institution))
                {
                    current.Institution = text;
                }
            }
        }

        if (current != null)
            education.Add(current);

        return education;
    }

    private List<Skill> ParseSkills(string text)
    {
        var skills = new List<Skill>();

        // Split by common delimiters
        var skillStrings = Regex.Split(text, @"[,;•\-\|\n]+")
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 1 && s.Length < 50)
            .Distinct();

        int order = 0;
        foreach (var skill in skillStrings)
        {
            // Skip section headers that might have been included
            if (SkillsHeaders.Any(h => skill.ToLower().Equals(h)))
                continue;

            skills.Add(new Skill
            {
                Order = order++,
                Name = skill,
                Level = SkillLevel.Intermediate
            });
        }

        return skills;
    }

    private List<Certification> ParseCertifications(List<TextBlock> lines)
    {
        var certifications = new List<Certification>();
        Certification? current = null;

        foreach (var line in lines)
        {
            var text = line.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            // New certification if line is bold or contains a year
            var yearMatch = Regex.Match(text, @"\b(19|20)\d{2}\b");

            if (line.IsBold || yearMatch.Success)
            {
                if (current != null)
                    certifications.Add(current);

                current = new Certification
                {
                    Order = certifications.Count,
                    Name = Regex.Replace(text, @"\b(19|20)\d{2}\b", "").Trim(' ', '-', '–', '|', ',')
                };

                if (yearMatch.Success)
                    current.IssueDate = new DateTime(int.Parse(yearMatch.Value), 1, 1);
            }
            else if (current != null && string.IsNullOrEmpty(current.IssuingOrganization))
            {
                current.IssuingOrganization = text;
            }
        }

        if (current != null)
            certifications.Add(current);

        return certifications;
    }

    private List<Project> ParseProjects(List<TextBlock> lines)
    {
        var projects = new List<Project>();
        Project? current = null;
        var descBuilder = new StringBuilder();

        foreach (var line in lines)
        {
            var text = line.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            if (line.IsBold || (text.Length < 60 && !text.StartsWith("•") && !text.StartsWith("-")))
            {
                if (current != null)
                {
                    current.Description = CleanText(descBuilder.ToString());
                    projects.Add(current);
                    descBuilder.Clear();
                }

                current = new Project
                {
                    Order = projects.Count,
                    Name = text
                };

                // Extract URL if present
                var urlMatch = Regex.Match(text, @"https?://[^\s]+");
                if (urlMatch.Success)
                {
                    current.Url = urlMatch.Value;
                    current.Name = text.Replace(urlMatch.Value, "").Trim(' ', '-', '|');
                }
            }
            else if (current != null)
            {
                var cleanedText = Regex.Replace(text, @"^[\s•\-\*]+", "").Trim();
                descBuilder.AppendLine(cleanedText);

                // Look for technologies
                if (text.ToLower().Contains("technolog") || text.ToLower().Contains("built with") || text.ToLower().Contains("stack"))
                {
                    var techText = Regex.Replace(text, @"^.*?:\s*", "");
                    current.Technologies = Regex.Split(techText, @"[,;]+")
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length < 30)
                        .ToList();
                }
            }
        }

        if (current != null)
        {
            current.Description = CleanText(descBuilder.ToString());
            projects.Add(current);
        }

        return projects;
    }

    private List<Language> ParseLanguages(string text)
    {
        var languages = new List<Language>();

        var lines = text.Split('\n');
        int order = 0;

        foreach (var line in lines)
        {
            var parts = Regex.Split(line, @"[-–—:]+");
            if (parts.Length >= 1)
            {
                var langName = parts[0].Trim();
                if (string.IsNullOrWhiteSpace(langName) || langName.Length > 30)
                    continue;

                var lang = new Language
                {
                    Order = order++,
                    Name = langName,
                    Proficiency = LanguageProficiency.Professional
                };

                // Try to parse proficiency
                if (parts.Length >= 2)
                {
                    var profText = parts[1].ToLower();
                    if (profText.Contains("native") || profText.Contains("bilingual"))
                        lang.Proficiency = LanguageProficiency.Native;
                    else if (profText.Contains("fluent") || profText.Contains("full"))
                        lang.Proficiency = LanguageProficiency.Fluent;
                    else if (profText.Contains("professional") || profText.Contains("working"))
                        lang.Proficiency = LanguageProficiency.Professional;
                    else if (profText.Contains("conversational") || profText.Contains("intermediate"))
                        lang.Proficiency = LanguageProficiency.Conversational;
                    else if (profText.Contains("basic") || profText.Contains("elementary"))
                        lang.Proficiency = LanguageProficiency.Basic;
                }

                languages.Add(lang);
            }
        }

        return languages;
    }

    private DateTime? ParseDate(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        dateStr = dateStr.Trim();

        // Try various formats
        var formats = new[] { "MMM yyyy", "MMMM yyyy", "MM/yyyy", "yyyy-MM", "yyyy" };
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateStr, format, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
                return date;
        }

        // Try just year
        if (int.TryParse(dateStr, out var year) && year >= 1950 && year <= 2100)
            return new DateTime(year, 1, 1);

        return null;
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        // Remove multiple spaces and normalize line breaks
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private class TextBlock
    {
        public string Text { get; set; } = "";
        public double Y { get; set; }
        public double FontSize { get; set; }
        public bool IsBold { get; set; }
    }

    private class ResumeSection
    {
        public string Header { get; set; } = "";
        public List<TextBlock> Lines { get; set; } = new();
    }
}
