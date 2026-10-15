using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Data;

public interface IJobApplicationRepository
{
    Task<List<JobApplication>> GetAllAsync();
    Task<List<JobApplication>> GetForResumeAsync(int resumeId);
    Task<JobApplication?> GetByIdAsync(int id);
    Task<JobApplication> CreateAsync(JobApplication application);
    Task<JobApplication> UpdateAsync(JobApplication application);
    Task DeleteAsync(int id);
}

public class JobApplicationRepository : IJobApplicationRepository
{
    private readonly IResumeDbContextFactory _factory;

    public JobApplicationRepository(IResumeDbContextFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Newest activity first. Ordered by when it was applied rather than created, so a job saved
    /// weeks ago and applied to yesterday sits where the user expects; unapplied rows sort to the
    /// top because they are the ones still needing a decision.
    /// </summary>
    public async Task<List<JobApplication>> GetAllAsync()
    {
        await using var context = _factory.CreateDbContext();
        return await context.JobApplications
            .AsNoTracking()
            .OrderByDescending(a => a.AppliedOn == null)
            .ThenByDescending(a => a.AppliedOn)
            .ThenByDescending(a => a.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<JobApplication>> GetForResumeAsync(int resumeId)
    {
        await using var context = _factory.CreateDbContext();
        return await context.JobApplications
            .AsNoTracking()
            .Where(a => a.ResumeId == resumeId)
            .OrderByDescending(a => a.AppliedOn)
            .ToListAsync();
    }

    public async Task<JobApplication?> GetByIdAsync(int id)
    {
        await using var context = _factory.CreateDbContext();
        return await context.JobApplications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<JobApplication> CreateAsync(JobApplication application)
    {
        await using var context = _factory.CreateDbContext();

        application.CreatedAt = DateTime.UtcNow;
        application.UpdatedAt = application.CreatedAt;
        application.RowVersion = Guid.NewGuid();
        application.SentResumeJson = null;
        application.ResumeCapturedAt = null;

        // An application that is already sent needs a date, or "how long have they had this?"
        // has no answer. Defaulting here rather than in the model keeps the model honest for the
        // Saved case, which genuinely has no date.
        if (application.Status != ApplicationStatus.Saved && application.AppliedOn is null)
        {
            application.AppliedOn = DateTime.UtcNow;
        }

        if (application.Status != ApplicationStatus.Saved)
            await CaptureSentResumeAsync(context, application);

        context.JobApplications.Add(application);
        await context.SaveChangesAsync();
        return application;
    }

    public async Task<JobApplication> UpdateAsync(JobApplication application)
    {
        await using var context = _factory.CreateDbContext();

        var stored = await context.JobApplications.FindAsync(application.Id)
            ?? throw new InvalidOperationException("This application no longer exists.");
        var wasDraft = stored.Status == ApplicationStatus.Saved;
        context.Entry(stored).Property(a => a.RowVersion).OriginalValue = application.RowVersion;
        stored.Company = application.Company;
        stored.Role = application.Role;
        stored.Status = application.Status;
        stored.Link = application.Link;
        stored.Notes = application.Notes;
        stored.AppliedOn = application.AppliedOn ?? stored.AppliedOn;
        stored.UpdatedAt = DateTime.UtcNow;
        stored.RowVersion = Guid.NewGuid();

        if (stored.Status != ApplicationStatus.Saved && stored.AppliedOn is null)
            stored.AppliedOn = DateTime.UtcNow;

        if (wasDraft && stored.Status != ApplicationStatus.Saved && stored.SentResumeJson is null)
            await CaptureSentResumeAsync(context, stored);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("This application changed elsewhere. Reload it before saving your changes.", ex);
        }
        return stored;
    }

    private static async Task CaptureSentResumeAsync(ResumeDbContext context, JobApplication application)
    {
        if (application.ResumeId is not { } resumeId)
            return;
        var resume = await context.Resumes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == resumeId);
        if (resume != null)
        {
            application.SentResumeJson = JsonSerializer.Serialize(resume);
            application.ResumeCapturedAt = DateTime.UtcNow;
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = _factory.CreateDbContext();
        var existing = await context.JobApplications.FirstOrDefaultAsync(a => a.Id == id);
        if (existing is null)
        {
            return;
        }

        context.JobApplications.Remove(existing);
        await context.SaveChangesAsync();
    }
}
