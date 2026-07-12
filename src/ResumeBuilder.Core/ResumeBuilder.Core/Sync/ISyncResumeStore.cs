using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Core.Sync;

/// <summary>
/// The slice of resume persistence that sync needs. Core cannot reference the Data project, so the
/// repository implements this and hands itself to the sync service.
/// </summary>
public interface ISyncResumeStore
{
    Task<List<Resume>> GetAllAsync();
    Task<Resume?> GetByIdAsync(int id);
    Task<Resume> CreateAsync(Resume resume);
    Task<Resume> UpdateAsync(Resume resume);
}
