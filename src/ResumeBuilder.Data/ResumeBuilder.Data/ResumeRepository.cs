using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Data;

/// <summary>Thrown when a resume was changed by someone else between load and save.</summary>
public class ResumeConcurrencyException : Exception
{
    public ResumeConcurrencyException(int resumeId)
        : base($"Resume {resumeId} was modified elsewhere since it was loaded. Reload it and re-apply your changes.")
    {
        ResumeId = resumeId;
    }

    public int ResumeId { get; }
}

/// <summary>
/// Each operation runs against its own short-lived context. This keeps saves from the UI thread and
/// the autosave timer from sharing a (non-thread-safe) DbContext, and removes the need to hand-manage
/// entity tracking state between calls.
/// </summary>
public class ResumeRepository : IResumeRepository
{
    private readonly IResumeDbContextFactory _factory;

    public ResumeRepository(IResumeDbContextFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Builds a factory from an existing context's connection so callers can keep passing one.</summary>
    public ResumeRepository(ResumeDbContext context)
        : this(new ResumeDbContextFactory(
            new DbContextOptionsBuilder<ResumeDbContext>()
                .UseSqlite(context.Database.GetConnectionString()!)
                .Options))
    {
    }

    public async Task<List<Resume>> GetAllAsync()
    {
        await using var context = _factory.CreateDbContext();
        var resumes = await context.Resumes
            .AsNoTracking()
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();

        foreach (var resume in resumes)
        {
            resume.SectionOrder.EnsureAllSectionsPresent();
        }

        return resumes;
    }

    public async Task<Resume?> GetByIdAsync(int id)
    {
        await using var context = _factory.CreateDbContext();
        var resume = await context.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        resume?.SectionOrder.EnsureAllSectionsPresent();
        return resume;
    }

    public async Task<Resume> CreateAsync(Resume resume)
    {
        await using var context = _factory.CreateDbContext();

        resume.CreatedAt = DateTime.UtcNow;
        resume.UpdatedAt = DateTime.UtcNow;
        resume.RowVersion = Guid.NewGuid();
        resume.SyncLegacyStyling();

        context.Resumes.Add(resume);
        await context.SaveChangesAsync();

        return resume;
    }

    public async Task<Resume> UpdateAsync(Resume resume)
    {
        await using var context = _factory.CreateDbContext();

        resume.UpdatedAt = DateTime.UtcNow;
        resume.SyncLegacyStyling();

        // Match on the version we loaded, then rotate it, so a concurrent writer's save fails
        // rather than silently overwriting ours (or vice versa).
        var loadedVersion = resume.RowVersion;
        resume.RowVersion = Guid.NewGuid();

        var entry = context.Resumes.Update(resume);
        entry.Property(r => r.RowVersion).OriginalValue = loadedVersion;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            resume.RowVersion = loadedVersion;
            throw new ResumeConcurrencyException(resume.Id);
        }

        return resume;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = _factory.CreateDbContext();

        var resume = await context.Resumes.FindAsync(id);
        if (resume != null)
        {
            context.Resumes.Remove(resume);
            await context.SaveChangesAsync();
        }
    }

    public async Task<Resume> DuplicateAsync(int id)
    {
        await using var context = _factory.CreateDbContext();

        var original = await context.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (original == null)
        {
            throw new InvalidOperationException($"Resume with id {id} not found");
        }

        var clone = DeepClone(original);
        ResetIdentity(clone);
        clone.Name = $"{original.Name} (Copy)";
        clone.CreatedAt = DateTime.UtcNow;
        clone.UpdatedAt = DateTime.UtcNow;
        clone.RowVersion = Guid.NewGuid();

        context.Resumes.Add(clone);
        await context.SaveChangesAsync();

        return clone;
    }

    public async Task<Resume> CreateVariantAsync(int baseResumeId, string targetRole, string jobDescription)
    {
        await using var context = _factory.CreateDbContext();

        var original = await context.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == baseResumeId);

        if (original == null)
        {
            throw new InvalidOperationException($"Resume with id {baseResumeId} not found");
        }

        var variant = DeepClone(original);
        ResetIdentity(variant);

        // Variants branch from a base resume, never from another variant, so the tree stays flat.
        variant.BaseResumeId = original.BaseResumeId ?? original.Id;
        variant.TargetRole = targetRole;
        variant.JobDescription = jobDescription;
        variant.Name = string.IsNullOrWhiteSpace(targetRole)
            ? $"{original.Name} (Variant)"
            : $"{original.Name} - {targetRole}";
        variant.CreatedAt = DateTime.UtcNow;
        variant.UpdatedAt = DateTime.UtcNow;

        context.Resumes.Add(variant);
        await context.SaveChangesAsync();

        return variant;
    }

    public async Task<List<Resume>> GetVariantsAsync(int baseResumeId)
    {
        await using var context = _factory.CreateDbContext();

        var variants = await context.Resumes
            .AsNoTracking()
            .Where(r => r.BaseResumeId == baseResumeId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();

        foreach (var variant in variants)
        {
            variant.SectionOrder.EnsureAllSectionsPresent();
        }

        return variants;
    }

    public static Resume DeepClone(Resume resume)
    {
        var json = JsonSerializer.Serialize(resume);
        return JsonSerializer.Deserialize<Resume>(json)!;
    }

    /// <summary>
    /// Clears the primary keys on a resume and everything under it, so it saves as a new record
    /// instead of overwriting whichever row happens to own those ids. Also mints a fresh
    /// <see cref="Resume.SyncId"/> — a copy that kept the original's would be treated as the same
    /// resume by sync and overwrite it.
    /// </summary>
    public static void ResetIdentity(Resume resume)
    {
        resume.Id = 0;
        resume.RowVersion = Guid.NewGuid();
        resume.SyncId = Guid.NewGuid();
        resume.PersonalInfo.Id = 0;

        foreach (var item in resume.Experiences) item.Id = 0;
        foreach (var item in resume.EducationList) item.Id = 0;
        foreach (var item in resume.Skills) item.Id = 0;
        foreach (var item in resume.Languages) item.Id = 0;
        foreach (var item in resume.Certifications) item.Id = 0;
        foreach (var item in resume.Projects) item.Id = 0;

        foreach (var section in resume.CustomSections)
        {
            section.Id = 0;
            foreach (var item in section.Items) item.Id = 0;
        }
    }
}
