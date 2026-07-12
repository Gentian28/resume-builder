using Microsoft.EntityFrameworkCore;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Data;

public interface ICoverLetterRepository
{
    Task<List<CoverLetter>> GetAllAsync();
    Task<List<CoverLetter>> GetForResumeAsync(int resumeId);
    Task<CoverLetter?> GetByIdAsync(int id);
    Task<CoverLetter> CreateAsync(CoverLetter letter);
    Task<CoverLetter> UpdateAsync(CoverLetter letter);
    Task DeleteAsync(int id);
}

public class CoverLetterRepository : ICoverLetterRepository
{
    private readonly IResumeDbContextFactory _factory;

    public CoverLetterRepository(IResumeDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<CoverLetter>> GetAllAsync()
    {
        await using var context = _factory.CreateDbContext();
        return await context.CoverLetters
            .AsNoTracking()
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<CoverLetter>> GetForResumeAsync(int resumeId)
    {
        await using var context = _factory.CreateDbContext();
        return await context.CoverLetters
            .AsNoTracking()
            .Where(c => c.ResumeId == resumeId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<CoverLetter?> GetByIdAsync(int id)
    {
        await using var context = _factory.CreateDbContext();
        return await context.CoverLetters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CoverLetter> CreateAsync(CoverLetter letter)
    {
        await using var context = _factory.CreateDbContext();

        letter.CreatedAt = DateTime.UtcNow;
        letter.UpdatedAt = DateTime.UtcNow;
        letter.RowVersion = Guid.NewGuid();

        context.CoverLetters.Add(letter);
        await context.SaveChangesAsync();

        return letter;
    }

    public async Task<CoverLetter> UpdateAsync(CoverLetter letter)
    {
        await using var context = _factory.CreateDbContext();

        letter.UpdatedAt = DateTime.UtcNow;

        var loadedVersion = letter.RowVersion;
        letter.RowVersion = Guid.NewGuid();

        var entry = context.CoverLetters.Update(letter);
        entry.Property(c => c.RowVersion).OriginalValue = loadedVersion;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            letter.RowVersion = loadedVersion;
            throw new ResumeConcurrencyException(letter.Id);
        }

        return letter;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = _factory.CreateDbContext();

        var letter = await context.CoverLetters.FindAsync(id);
        if (letter != null)
        {
            context.CoverLetters.Remove(letter);
            await context.SaveChangesAsync();
        }
    }
}
