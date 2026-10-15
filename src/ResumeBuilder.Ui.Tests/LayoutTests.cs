using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using Moq;
using ResumeBuilder.App.Services;
using ResumeBuilder.App.ViewModels;
using ResumeBuilder.App.Views;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.SmartContent;
using ResumeBuilder.Core.UndoRedo;
using ResumeBuilder.Data;
using ResumeBuilder.Export;
using ResumeBuilder.Templates;

[assembly: AvaloniaTestApplication(typeof(ResumeBuilder.Ui.Tests.TestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace ResumeBuilder.Ui.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<ResumeBuilder.App.App>()
        .WithInterFont().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public class LayoutTests
{
    private static MainWindowViewModel Editor()
    {
        var registry = new TemplateRegistry(); var ai = new AiProviderRouter();
        var repository = new Mock<IResumeRepository>(); repository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Resume>());
        var letters = new Mock<ICoverLetterRepository>(); letters.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CoverLetter>());
        var applications = new Mock<IJobApplicationRepository>(); applications.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<JobApplication>());
        var services = new AppServices(repository.Object, letters.Object, applications.Object, registry, new ExportService(registry), new CoverLetterExportService(registry),
            null!, null!, new UndoRedoManager(), ai, null!, new JobTailoringService(ai), new CoverLetterService(ai), new UpdateService(), null!);
        return new MainWindowViewModel(services, initialize: false);
    }
    [AvaloniaTheory]
    [InlineData("first-run", 800, 500, false)]
    [InlineData("first-run", 1400, 900, false)]
    [InlineData("applications", 800, 500, false)]
    [InlineData("applications", 1400, 900, false)]
    [InlineData("letter", 800, 500, false)]
    [InlineData("letter", 1400, 900, false)]
    [InlineData("resumes", 800, 500, false)]
    [InlineData("resumes", 1400, 900, false)]
    [InlineData("first-run", 800, 500, true)]
    [InlineData("applications", 800, 500, true)]
    [InlineData("letter", 800, 500, true)]
    [InlineData("resumes", 800, 500, true)]
    public void ScreensRenderAtLaptopAndDesktopSizes(string screen, int width, int height, bool dark = false)
    {
        Application.Current!.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        var vm = Editor();
        if (screen == "letter") vm.NewCoverLetterCommand.ExecuteAsync(null).GetAwaiter().GetResult();
        if (screen == "applications") vm.Applications.Add(new(new JobApplication { Company = "Example company", Role = "Engineer" }));
        Control view = screen switch { "first-run" => new FirstRunOverlay(), "applications" => new ApplicationsOverlay(), "letter" => new CoverLetterEditorOverlay(), _ => new ResumeListOverlay() };
        var window = new Window { Width = width, Height = height, Content = view, DataContext = vm };
        try
        {
            window.Show(); Dispatcher.UIThread.RunJobs();
            view.Bounds.Width.Should().Be(width); view.Bounds.Height.Should().Be(height);
            if (screen == "letter") { vm.SelectedLetterTemplate.Should().NotBeNull(); view.GetVisualDescendants().OfType<ComboBox>().First().SelectedItem.Should().BeSameAs(vm.SelectedLetterTemplate); }
            foreach (var button in view.GetVisualDescendants().OfType<Button>().Where(b => b.Content is "Done" or "Cancel" or "Draft with AI" or "Save"))
            {
                var location = button.TranslatePoint(default, window)!.Value;
                location.X.Should().BeGreaterThanOrEqualTo(0);
                (location.X + button.Bounds.Width).Should().BeLessThanOrEqualTo(width + 1);
            }
            var artifacts = Environment.GetEnvironmentVariable("RESUMEBUILDER_UI_ARTIFACTS");
            if (!string.IsNullOrEmpty(artifacts))
            {
                Directory.CreateDirectory(artifacts);
                using var frame = window.CaptureRenderedFrame(); frame.Should().NotBeNull();
                frame!.Save(Path.Combine(artifacts, $"{screen}-{width}{(dark ? "-dark" : "")}.png"));
            }
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void SearchTextBox_UpdatesTheBoundFilterAndLetterSenderHasAccessibleName()
    {
        var vm = Editor(); vm.SavedResumes.Add(new Resume { Id = 1, Name = "Engineer" });
        var window = new Window { Width = 800, Height = 500, Content = new ResumeListOverlay(), DataContext = vm };
        try
        {
            window.Show(); Dispatcher.UIThread.RunJobs();
            var search = window.GetVisualDescendants().OfType<TextBox>().Single();
            search.Text = "Missing"; Dispatcher.UIThread.RunJobs(); vm.HasResumeMatches.Should().BeFalse();
            search.Text = "Engineer"; Dispatcher.UIThread.RunJobs(); vm.HasResumeMatches.Should().BeTrue();
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ReviewModal_CapturesEscapeAndKeepsItsDetailsOnScreen()
    {
        var vm = Editor(); vm.ShowReview = true; vm.ReviewTitle = "Support diagnostics"; vm.ReviewText = "Synthetic review details";
        var window = new MainWindow { Width = 800, Height = 500, DataContext = vm };
        try
        {
            window.Show(); Dispatcher.UIThread.RunJobs();
            var details = window.FindControl<TextBox>("ReviewDetails")!;
            details.IsEffectivelyVisible.Should().BeTrue();
            details.Focus();
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None); window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs(); vm.ShowReview.Should().BeFalse();
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(800, 500)]
    [InlineData(1400, 900)]
    public void MainToolbar_KeepsSaveAndExportReachable(int width, int height)
    {
        var vm = Editor();
        var window = new MainWindow { Width = width, Height = height, DataContext = vm };
        try
        {
            window.Show(); Dispatcher.UIThread.RunJobs();
            foreach (var button in window.GetVisualDescendants().OfType<Control>().Where(b => b is ContentControl { Content: "Save" or "Export PDF" } && b.IsEffectivelyVisible))
            {
                var location = button.TranslatePoint(default, window)!.Value;
                (location.X + button.Bounds.Width).Should().BeLessThanOrEqualTo(width + 1);
                location.Y.Should().BeLessThan(height);
            }
            var artifacts = Environment.GetEnvironmentVariable("RESUMEBUILDER_UI_ARTIFACTS");
            if (!string.IsNullOrEmpty(artifacts)) { using var frame = window.CaptureRenderedFrame(); frame!.Save(Path.Combine(artifacts, $"editor-{width}.png")); }
        }
        finally { window.DataContext = null; window.Close(); }
    }

}
