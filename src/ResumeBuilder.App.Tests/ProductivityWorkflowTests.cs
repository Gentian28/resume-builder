using System.Text.Json;
using AwesomeAssertions;
using ResumeBuilder.App.ViewModels;
using ResumeBuilder.Core.Editing;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.App.Tests;

public partial class EditorWorkflowTests
{
    [Fact]
    public async Task BundledExample_OpensWithoutReplacingExistingSavedWork()
    {
        var existing = await _resumes.CreateAsync(new Resume { Name = "Existing", Summary = "Keep me" });
        var vm = Editor();
        await vm.OpenExampleCommand.ExecuteAsync(null);
        vm.FirstName.Should().Be("Jane");
        vm.CurrentResume.Id.Should().Be(0);
        vm.HasImportReview.Should().BeTrue();
        vm.ShowFirstRun.Should().BeFalse();
        (await _resumes.GetByIdAsync(existing.Id))!.Summary.Should().Be("Keep me");
        vm.DismissImportReviewCommand.Execute(null);
        vm.HasImportReview.Should().BeFalse();
        vm.FirstName.Should().Be("Jane");
    }
    [Fact]
    public async Task LetterControls_SaveSenderTemplateAndSourceAndUndoSourceChange()
    {
        var first = await _resumes.CreateAsync(new Resume { Name = "First", PersonalInfo = new() { FirstName = "Jane" } });
        var second = await _resumes.CreateAsync(new Resume { Name = "Second", PersonalInfo = new() { FirstName = "Alex" } });
        var vm = Editor();
        await vm.LoadResumeCommand.ExecuteAsync(first);
        await vm.NewCoverLetterCommand.ExecuteAsync(null);
        vm.LetterSenderEmail = "letter@example.com";
        vm.SelectedLetterTemplate = vm.LetterTemplates.Last();
        vm.LetterPaperSize = PaperSize.Letter;
        await vm.SaveActiveDocumentCommand.ExecuteAsync(null);
        var saved = (await _letters.GetAllAsync()).Single();
        saved.PersonalInfo.Email.Should().Be("letter@example.com");
        saved.SelectedTemplateId.Should().Be(vm.LetterTemplates.Last().Id);
        saved.TemplateSettings.PaperSize.Should().Be(PaperSize.Letter);
        await vm.LoadResumeCommand.ExecuteAsync(second);
        await vm.UseOpenResumeForLetterCommand.ExecuteAsync(null);
        vm.LetterSenderFirstName.Should().Be("Alex");
        vm.UndoCommand.Execute(null);
        vm.LetterSenderFirstName.Should().Be("Jane");
        vm.LetterSenderEmail.Should().Be("letter@example.com");
    }
    [Fact]
    public async Task ApplicationContext_CapturesLinkedLetterWhenSentAndPreservesIt()
    {
        var resume = await _resumes.CreateAsync(new Resume { Summary = "Resume sent" });
        var letter = await _letters.CreateAsync(CoverLetter.FromResume(resume));
        letter.Paragraphs = ["Letter sent"];
        letter = await _letters.UpdateAsync(letter);
        var repository = new ResumeBuilder.Data.JobApplicationRepository(_factory, new ResumeBuilder.Export.SentDocumentRenderer(new ResumeBuilder.Templates.TemplateRegistry()));
        var application = await repository.CreateAsync(new JobApplication { ResumeId = resume.Id, CoverLetterId = letter.Id, Status = ApplicationStatus.Saved });
        application.SentCoverLetterJson.Should().BeNull();
        application.Contact = "Recruiter";
        application.NextActionOn = DateTime.Today.AddDays(-1);
        application.Status = ApplicationStatus.Applied;
        application = await repository.UpdateAsync(application);
        JsonSerializer.Deserialize<CoverLetter>(application.SentCoverLetterJson!)!.Paragraphs.Should().Equal("Letter sent");
        application.SentResumePdf.Should().NotBeNullOrEmpty();
        application.SentCoverLetterPdf.Should().NotBeNullOrEmpty();
        var sentPdf = application.SentCoverLetterPdf!.ToArray();
        letter.Paragraphs = ["Changed later"]; await _letters.UpdateAsync(letter);
        application.Notes = "Interview";
        application = await repository.UpdateAsync(application);
        JsonSerializer.Deserialize<CoverLetter>(application.SentCoverLetterJson!)!.Paragraphs.Should().Equal("Letter sent");
        application.SentCoverLetterPdf.Should().Equal(sentPdf);
        var vm = Editor(); await vm.OpenApplicationsCommand.ExecuteAsync(null);
        vm.ApplicationFilter = "Follow-up due";
        vm.FilteredApplications.Should().ContainSingle();
        await vm.OpenApplicationLetterCommand.ExecuteAsync(vm.Applications.Single());
        vm.CurrentLetter.Id.Should().Be(0);
        vm.LetterParagraphs.Single().Text.Should().Be("Letter sent");
    }
    [Fact]
    public async Task Search_KeepsMatchingVariantsWithTheirBase()
    {
        var parent = await _resumes.CreateAsync(new Resume { Name = "Base" });
        var variant = await _resumes.CreateVariantAsync(parent.Id, "Developer", "Posting");
        var vm = Editor();
        await (Task)Invoke(vm, "LoadSavedResumesAsync")!;
        vm.ResumeSearch = "Developer";
        vm.FilteredResumes.Select(r => r.Id).Should().Equal(parent.Id, variant.Id);
        vm.ResumeSearch = "Missing";
        vm.HasResumeMatches.Should().BeFalse();
    }
    [Fact]
    public async Task ExportReview_ContainsExtractedTextAndDiagnosticsExcludePersonalContent()
    {
        var vm = Editor(); vm.FirstName = "Synthetic private name"; vm.Summary = "Synthetic private summary"; vm.GitHub = "janedoe";
        await vm.ReviewExportCommand.ExecuteAsync(null);
        vm.ReviewText.Should().Contain("Synthetic private summary").And.Contain("page(s)");
        vm.ReviewText.Should().NotContain("GitHub: Invalid URL");
        vm.ReviewText.ReplaceLineEndings("\n").Should().Contain("Synthetic private name\n");
        await _dialogs.ShowErrorAsync("Save failed", "secret content");
        vm.ShowSupportDiagnosticsCommand.Execute(null);
        vm.ReviewText.Should().Contain("ResumeBuilder").And.NotContain("Synthetic private").And.NotContain("secret content").And.NotContain(_folder);
    }
}
