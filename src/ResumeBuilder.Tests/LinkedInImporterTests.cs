using System.IO.Compression;
using System.Text;
using AwesomeAssertions;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export.Importers;

namespace ResumeBuilder.Tests;

/// <summary>
/// A LinkedIn data export is a zip of CSVs whose file names and columns LinkedIn has changed more
/// than once. The importer had no tests at all; these pin what a typical export must produce.
/// </summary>
public class LinkedInImporterTests
{
    private const string Profile =
        "First Name,Last Name,Maiden Name,Address,Birth Date,Headline,Summary,Industry,Zip Code,Geo Location,Twitter Handles,Websites,Instant Messengers\n" +
        "Ann,Example,,12 High Street,,Senior Engineer at Acme,\"Builds things.\nCarefully.\",Software,SW1A 1AA,\"London, United Kingdom\",,\"https://example.com,https://linkedin.com/in/ann,https://github.com/ann\",\n";

    private const string Positions =
        "Company Name,Title,Description,Location,Started On,Finished On\n" +
        "Acme,Senior Engineer,Leads the platform team.,London,Mar 2021,\n" +
        "Old Co,Engineer,Shipped the first product.,Remote,Jan 2018,Feb 2021\n";

    private const string Education =
        "School Name,Start Date,End Date,Notes,Degree Name,Activities\n" +
        "State University,2012,2015,First class,BSc Computer Science,Chess club\n";

    private const string Skills = "Name\nC#\nKubernetes\n\n";

    private const string Languages =
        "Name,Proficiency\n" +
        "English,Native or Bilingual\n" +
        "Italian,Limited Working\n";

    private const string Emails =
        "Email Address,Confirmed,Primary,Updated On\n" +
        "ann@example.com,Yes,Yes,2024-01-01\n";

    private static MemoryStream Zip(Dictionary<string, string> files)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private static Dictionary<string, string> TypicalExport(string prefix = "") => new()
    {
        [prefix + "Profile.csv"] = Profile,
        [prefix + "Positions.csv"] = Positions,
        [prefix + "Education.csv"] = Education,
        [prefix + "Skills.csv"] = Skills,
        [prefix + "Languages.csv"] = Languages,
        [prefix + "Email Addresses.csv"] = Emails,
    };

    [Fact]
    public async Task TypicalExport_MapsProfilePositionsEducationSkillsAndLanguages()
    {
        var result = await new LinkedInImporter().ImportAsync(Zip(TypicalExport()));

        result.Success.Should().BeTrue(result.ErrorMessage);
        var resume = result.Data!;

        resume.PersonalInfo.FirstName.Should().Be("Ann");
        resume.PersonalInfo.LastName.Should().Be("Example");
        resume.PersonalInfo.JobTitle.Should().Be("Senior Engineer at Acme");
        resume.Summary.Should().Contain("Builds things.");
        resume.PersonalInfo.City.Should().Be("London");
        resume.PersonalInfo.Country.Should().Be("United Kingdom");
        resume.PersonalInfo.Website.Should().Be("https://example.com");
        resume.PersonalInfo.LinkedIn.Should().Be("https://linkedin.com/in/ann");
        resume.PersonalInfo.GitHub.Should().Be("https://github.com/ann");
        resume.PersonalInfo.Email.Should().Be("ann@example.com");

        resume.Experiences.Should().HaveCount(2);
        var current = resume.Experiences.Single(e => e.Company == "Acme");
        current.JobTitle.Should().Be("Senior Engineer");
        current.StartDate.Should().Be(new DateTime(2021, 3, 1));
        current.EndDate.Should().BeNull();
        current.IsCurrentRole.Should().BeFalse("a blank Finished On is missing data, not a statement that the role is current");
        var previous = resume.Experiences.Single(e => e.Company == "Old Co");
        previous.EndDate.Should().Be(new DateTime(2021, 2, 1));

        var degree = resume.EducationList.Single();
        degree.Institution.Should().Be("State University");
        degree.Degree.Should().Be("BSc Computer Science");
        degree.StartDate.Should().Be(new DateTime(2012, 1, 1));
        degree.EndDate.Should().Be(new DateTime(2015, 1, 1));

        resume.Skills.Select(s => s.Name).Should().BeEquivalentTo(new[] { "C#", "Kubernetes" }, "a blank row is not a skill");

        resume.Languages.Single(l => l.Name == "English").Proficiency.Should().Be(LanguageProficiency.Native);
        resume.Languages.Single(l => l.Name == "Italian").Proficiency.Should().Be(LanguageProficiency.Conversational);
    }

    [Fact]
    public async Task FilesInsideAFolder_AreStillFound()
    {
        // Exports unzip into a folder, and people re-zip that folder rather than its contents.
        var result = await new LinkedInImporter().ImportAsync(Zip(TypicalExport("Basic_LinkedInDataExport/")));

        result.Success.Should().BeTrue();
        result.Data!.PersonalInfo.FirstName.Should().Be("Ann");
        result.Data.Experiences.Should().HaveCount(2);
    }

    [Fact]
    public async Task PhoneNumbers_AreReadUnderEitherFileName()
    {
        const string phones = "Extension,Number,Type\n,+44 20 7946 0000,Mobile\n";

        var spaced = await new LinkedInImporter().ImportAsync(Zip(new() { ["Profile.csv"] = Profile, ["Phone Numbers.csv"] = phones }));
        var joined = await new LinkedInImporter().ImportAsync(Zip(new() { ["Profile.csv"] = Profile, ["PhoneNumbers.csv"] = phones }));

        spaced.Data!.PersonalInfo.Phone.Should().Be("+44 20 7946 0000");
        joined.Data!.PersonalInfo.Phone.Should().Be("+44 20 7946 0000");
    }

    [Fact]
    public async Task MissingFiles_ProduceWarningsNotFailure()
    {
        var result = await new LinkedInImporter().ImportAsync(Zip(new() { ["Skills.csv"] = Skills }));

        result.Success.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Profile.csv"));
        result.Warnings.Should().Contain(w => w.Contains("Positions.csv"));
        result.Warnings.Should().Contain(w => w.Contains("Education.csv"));
        result.Data!.Skills.Should().HaveCount(2);
    }

    [Fact]
    public async Task AMalformedCsv_IsSkippedRatherThanFailingTheImport()
    {
        var files = TypicalExport();
        files["Positions.csv"] = "not,a,valid\nheader row at all";

        var result = await new LinkedInImporter().ImportAsync(Zip(files));

        result.Success.Should().BeTrue();
        result.Data!.PersonalInfo.FirstName.Should().Be("Ann");
        result.Data.Experiences.Should().BeEmpty();
    }

    [Fact]
    public async Task SomethingThatIsNotAZip_FailsWithAMessage()
    {
        var result = await new LinkedInImporter().ImportAsync(new MemoryStream(Encoding.UTF8.GetBytes("plain text")));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }
}
