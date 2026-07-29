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

        // An application that is already sent needs a date, or "how long have they had this?"
        // has no answer. Defaulting here rather than in the model keeps the model honest for the
        // Saved case, which genuinely has no date.
        if (application.Status != ApplicationStatus.Saved && application.AppliedOn is null)
        {
            application.AppliedOn = DateTime.UtcNow;
        }

        context.JobApplications.Add(application);
        await context.SaveChangesAsync();
        return application;
    }

    public async Task<JobApplication> UpdateAsync(JobApplication application)
    {
        await using var context = _factory.CreateDbContext();

        application.UpdatedAt = DateTime.UtcNow;

        // Moving off Saved is the moment it was actually sent.
        if (application.Status != ApplicationStatus.Saved && application.AppliedOn is null)
        {
            application.AppliedOn = DateTime.UtcNow;
        }

        context.JobApplications.Update(application);
        await context.SaveChangesAsync();
        return application;
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
