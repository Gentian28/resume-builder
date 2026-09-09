using System.Text;
using AwesomeAssertions;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export.Exporters;
using ResumeBuilder.Export.Importers;

namespace ResumeBuilder.Tests;

/// <summary>
/// The JSON Resume file this app writes must validate against the schema and must read back whole.
/// Those two pull in opposite directions (the schema has no field for "still here" or for a custom
/// section), and meta.resumeBuilder is where the difference is kept.
/// </summary>
public class JsonResumeRoundTripTests
{
    private static async Task<(string json, Resume back)> RoundTrip(Resume resume)
    {
        var bytes = await new JsonResumeExporter().ExportAsync(resume, "modern");
        var result = await new JsonResumeImporter().ImportAsync(new MemoryStream(bytes));
        result.Success.Should().BeTrue(result.ErrorMessage);
        return (Encoding.UTF8.GetString(bytes), result.Data!);
    }

    [Fact]
    public async Task AnOngoingRole_ExportsWithoutAnEndDate_AndComesBackOngoing()
    {
        var resume = new Resume();
        resume.Experiences.Add(new Experience { JobTitle = "Lead", Company = "Acme", StartDate = new DateTime(2022, 3, 1), IsCurrentRole = true });
        resume.Experiences.Add(new Experience { JobTitle = "Dev", Company = "Old", StartDate = new DateTime(2019, 1, 1), EndDate = new DateTime(2022, 2, 1), Order = 1 });
        resume.EducationList.Add(new Education { Institution = "Uni", Degree = "MSc", StartDate = new DateTime(2024, 9, 1), IsCurrentlyStudying = true });
        resume.Projects.Add(new Project { Name = "Side", StartDate = new DateTime(2025, 1, 1), IsOngoing = true });

        var (json, back) = await RoundTrip(resume);

        json.Should().NotContain("Present", "the schema wants an ongoing entry to omit endDate, not to hold a word in it");
        back.Experiences[0].IsCurrentRole.Should().BeTrue();
        back.Experiences[0].EndDate.Should().BeNull();
        back.Experiences[1].IsCurrentRole.Should().BeFalse();
        back.Experiences[1].EndDate.Should().Be(new DateTime(2022, 2, 1));
        back.EducationList[0].IsCurrentlyStudying.Should().BeTrue();
        back.Projects[0].IsOngoing.Should().BeTrue();
    }

    [Fact]
    public async Task AFileFromAnotherTool_WithNoEndDateAndNoExtension_IsStillNotTreatedAsOngoing()
    {
        // Missing data is not a statement that the role is current; only this app's own marker or
        // an explicit word like "Present" says so.
        const string json = """
            {"basics":{"name":"Ann Example"},"work":[{"name":"Acme","position":"Dev","startDate":"2020-01-01"}]}
            """;

        var result = await new JsonResumeImporter().ImportAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        result.Data!.Experiences.Single().IsCurrentRole.Should().BeFalse();
    }

    [Fact]
    public async Task YearOnlyAndYearMonthDates_Import()
    {
        const string json = """
            {"basics":{"name":"Ann Example"},
             "work":[{"name":"Acme","position":"Dev","startDate":"2019","endDate":"2021-06"}],
             "education":[{"institution":"Uni","studyType":"BSc","startDate":"2012","endDate":"2015"}]}
            """;

        var result = await new JsonResumeImporter().ImportAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        var work = result.Data!.Experiences.Single();
        work.StartDate.Should().Be(new DateTime(2019, 1, 1));
        work.EndDate.Should().Be(new DateTime(2021, 6, 1));
        result.Data.EducationList.Single().EndDate.Should().Be(new DateTime(2015, 1, 1));
    }

    [Fact]
    public async Task CustomSections_TheSchemaHasNoFieldFor_RoundTrip()
    {
        var resume = new Resume();
        resume.CustomSections.Add(new CustomSection
        {
            Title = "Speaking",
            Items =
            {
                new CustomSectionItem { Title = "Scaling Ingestion Pipelines", Subtitle = "DevConf", Description = "Keynote", StartDate = new DateTime(2024, 5, 14) },
                new CustomSectionItem { Title = "Workshop", Subtitle = "Meetup", Order = 1 },
            }
        });
        resume.CustomSections.Add(new CustomSection
        {
            Title = "Awards",
            Order = 1,
            Items = { new CustomSectionItem { Title = "Engineer of the Year", Subtitle = "Acme", StartDate = new DateTime(2023, 12, 1) } }
        });

        var (json, back) = await RoundTrip(resume);

        json.Should().Contain("\"awards\"", "a section the schema has a field for still uses that field");
        back.CustomSections.Should().HaveCount(2);
        var speaking = back.CustomSections.Single(s => s.Title == "Speaking");
        speaking.Items.Should().HaveCount(2);
        speaking.Items[0].Title.Should().Be("Scaling Ingestion Pipelines");
        speaking.Items[0].Subtitle.Should().Be("DevConf");
        speaking.Items[0].StartDate.Should().Be(new DateTime(2024, 5, 14));
        back.CustomSections.Single(s => s.Title == "Awards").Items.Single().Title.Should().Be("Engineer of the Year");
    }

    [Fact]
    public async Task AResumeWithNothingToExtend_WritesNoExtension()
    {
        var resume = new Resume();
        resume.Experiences.Add(new Experience { JobTitle = "Dev", Company = "Acme", StartDate = new DateTime(2019, 1, 1), EndDate = new DateTime(2020, 1, 1) });

        var (json, _) = await RoundTrip(resume);

        json.Should().NotContain("resumeBuilder");
    }
}
