using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.Sync;

namespace ResumeBuilder.Data;

/// <summary>
/// Full resume persistence. Extends <see cref="ISyncResumeStore"/> (get/create/update) so the sync
/// service in Core can drive persistence without Core depending on this project.
/// </summary>
public interface IResumeRepository : ISyncResumeStore
{
    Task DeleteAsync(int id);
    Task<Resume> DuplicateAsync(int id);

    /// <summary>Branches a resume into a variant tailored for one application.</summary>
    Task<Resume> CreateVariantAsync(int baseResumeId, string targetRole, string jobDescription);

    /// <summary>All variants branched from a resume, newest first.</summary>
    Task<List<Resume>> GetVariantsAsync(int baseResumeId);
}
