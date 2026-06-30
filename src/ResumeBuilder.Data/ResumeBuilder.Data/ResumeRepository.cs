using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Data;

public class ResumeRepository : IResumeRepository
{
    private readonly ResumeDbContext _context;

    public ResumeRepository(ResumeDbContext context)
    {
        _context = context;
    }

    public async Task<List<Resume>> GetAllAsync()
    {
        return await _context.Resumes
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Resume?> GetByIdAsync(int id)
    {
        return await _context.Resumes.FindAsync(id);
    }

    public async Task<Resume> CreateAsync(Resume resume)
    {
        resume.CreatedAt = DateTime.UtcNow;
        resume.UpdatedAt = DateTime.UtcNow;

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        // Detach after save to prevent tracking conflicts on future saves
        _context.Entry(resume).State = EntityState.Detached;

        return resume;
    }

    public async Task<Resume> UpdateAsync(Resume resume)
    {
        resume.UpdatedAt = DateTime.UtcNow;

        var entry = _context.Entry(resume);
        if (entry.State == EntityState.Detached)
        {
            // Entity not tracked - attach it as Modified
            _context.Resumes.Update(resume);
        }
        else
        {
            // Entity already tracked - mark as Modified
            entry.State = EntityState.Modified;
        }

        await _context.SaveChangesAsync();

        // Detach after save to prevent tracking conflicts on next save
        _context.Entry(resume).State = EntityState.Detached;

        return resume;
    }

    public async Task DeleteAsync(int id)
    {
        var resume = await _context.Resumes.FindAsync(id);
        if (resume != null)
        {
            _context.Resumes.Remove(resume);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Resume> DuplicateAsync(int id)
    {
        var original = await _context.Resumes.FindAsync(id);
        if (original == null)
        {
            throw new InvalidOperationException($"Resume with id {id} not found");
        }

        // Deep clone using JSON serialization
        var json = JsonSerializer.Serialize(original);
        var clone = JsonSerializer.Deserialize<Resume>(json)!;

        // Reset identity fields
        clone.Id = 0;
        clone.Name = $"{original.Name} (Copy)";
        clone.CreatedAt = DateTime.UtcNow;
        clone.UpdatedAt = DateTime.UtcNow;

        _context.Resumes.Add(clone);
        await _context.SaveChangesAsync();

        return clone;
    }
}
