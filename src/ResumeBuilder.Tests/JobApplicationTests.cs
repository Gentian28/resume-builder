using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Data;

namespace ResumeBuilder.Tests;

/// <summary>
/// The tracker answers one question well or it is not worth having: when a company calls, which
/// résumé did they read, and how long have they had it. These pin that.
/// </summary>
public class JobApplicationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly JobApplicationRepository _repository;

    public JobApplicationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ResumeDbContext>().UseSqlite(_connection).Options;
        using (var context = new ResumeDbContext(options))
        {
            context.Database.EnsureCreated();
        }

        _repository = new JobApplicationRepository(new TestFactory(options));
    }

    public void Dispose() => _connection.Dispose();

    private sealed class TestFactory : IResumeDbContextFactory
    {
        private readonly DbContextOptions<ResumeDbContext> _options;
        public TestFactory(DbContextOptions<ResumeDbContext> options) => _options = options;
        public ResumeDbContext CreateDbContext() => new(_options);
    }

    [Fact]
    public async Task Create_StampsTheDateItWasSent()
    {
        var saved = await _repository.CreateAsync(new JobApplication { Company = "Stripe", Role = "Backend" });

        // Without a date, "how long have they had this?" has no answer - which is most of why the
        // list is worth opening.
        saved.AppliedOn.Should().NotBeNull();
        saved.DaysSinceApplied.Should().Be(0);
    }

    [Fact]
    public async Task Create_LeavesASavedJobUndated()
    {
        var saved = await _repository.CreateAsync(new JobApplication
        {
            Company = "Figma",
            Status = ApplicationStatus.Saved
        });

        // A job you have not applied to has no application date; inventing one makes every
        // "silent for N days" reading wrong.
        saved.AppliedOn.Should().BeNull();
        saved.DaysSinceApplied.Should().BeNull();
    }

    [Fact]
    public async Task Update_DatesItWhenItStopsBeingMerelySaved()
    {
        var application = await _repository.CreateAsync(new JobApplication
        {
            Company = "Linear",
            Status = ApplicationStatus.Saved
        });

        application.Status = ApplicationStatus.Applied;
        var updated = await _repository.UpdateAsync(application);

        updated.AppliedOn.Should().NotBeNull("moving off Saved is the moment it was sent");
    }

    [Fact]
    public void IsStale_OnlyFlagsThingsActuallyWaitingOnSomeoneElse()
    {
        var waiting = new JobApplication
        {
            Status = ApplicationStatus.Applied,
            AppliedOn = DateTime.UtcNow.AddDays(-21)
        };
        var rejectedLongAgo = new JobApplication
        {
            Status = ApplicationStatus.Rejected,
            AppliedOn = DateTime.UtcNow.AddDays(-90)
        };
        var sentYesterday = new JobApplication
        {
            Status = ApplicationStatus.Applied,
            AppliedOn = DateTime.UtcNow.AddDays(-1)
        };

        waiting.IsStale().Should().BeTrue();
        // A closed application is not "needs chasing" however old it is.
        rejectedLongAgo.IsStale().Should().BeFalse();
        sentYesterday.IsStale().Should().BeFalse();
    }

    [Fact]
    public async Task GetForResume_FindsWhichApplicationsUsedAGivenResume()
    {
        await _repository.CreateAsync(new JobApplication { Company = "Stripe", ResumeId = 7 });
        await _repository.CreateAsync(new JobApplication { Company = "Figma", ResumeId = 7 });
        await _repository.CreateAsync(new JobApplication { Company = "Linear", ResumeId = 9 });

        var forSeven = await _repository.GetForResumeAsync(7);

        forSeven.Select(a => a.Company).Should().BeEquivalentTo(["Stripe", "Figma"]);
    }

    [Fact]
    public async Task GetAll_PutsUnappliedFirstThenNewestApplications()
    {
        await _repository.CreateAsync(new JobApplication
        {
            Company = "Old", AppliedOn = DateTime.UtcNow.AddDays(-30)
        });
        await _repository.CreateAsync(new JobApplication
        {
            Company = "Recent", AppliedOn = DateTime.UtcNow.AddDays(-1)
        });
        await _repository.CreateAsync(new JobApplication
        {
            Company = "NotSentYet", Status = ApplicationStatus.Saved
        });

        var all = await _repository.GetAllAsync();

        // Things still needing a decision come first; after that, most recent activity.
        all.Select(a => a.Company).Should().Equal("NotSentYet", "Recent", "Old");
    }

    [Fact]
    public async Task Delete_RemovesOnlyTheOneAsked()
    {
        var keep = await _repository.CreateAsync(new JobApplication { Company = "Keep" });
        var drop = await _repository.CreateAsync(new JobApplication { Company = "Drop" });

        await _repository.DeleteAsync(drop.Id);

        (await _repository.GetAllAsync()).Select(a => a.Company).Should().Equal("Keep");
        (await _repository.GetByIdAsync(keep.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Status_SurvivesARoundTripAsText()
    {
        var created = await _repository.CreateAsync(new JobApplication
        {
            Company = "Anthropic",
            Status = ApplicationStatus.Interviewing
        });

        var reloaded = await _repository.GetByIdAsync(created.Id);

        // Stored as a string, so reordering the enum later cannot silently reinterpret old rows.
        reloaded!.Status.Should().Be(ApplicationStatus.Interviewing);
    }
}
