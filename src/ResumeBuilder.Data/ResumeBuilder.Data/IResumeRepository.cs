using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Data;

public interface IResumeRepository
{
    Task<List<Resume>> GetAllAsync();
    Task<Resume?> GetByIdAsync(int id);
    Task<Resume> CreateAsync(Resume resume);
    Task<Resume> UpdateAsync(Resume resume);
    Task DeleteAsync(int id);
    Task<Resume> DuplicateAsync(int id);
}
