using Microsoft.EntityFrameworkCore;

namespace ResumeBuilder.Data;

/// <summary>
/// Hands out short-lived <see cref="ResumeDbContext"/> instances. A single long-lived context is
/// not thread-safe, and the app saves from both the UI thread and the autosave timer.
/// </summary>
public interface IResumeDbContextFactory
{
    ResumeDbContext CreateDbContext();
}

public class ResumeDbContextFactory : IResumeDbContextFactory
{
    private readonly DbContextOptions<ResumeDbContext>? _options;

    public ResumeDbContextFactory()
    {
    }

    public ResumeDbContextFactory(DbContextOptions<ResumeDbContext> options)
    {
        _options = options;
    }

    public ResumeDbContextFactory(string databasePath)
    {
        _options = new DbContextOptionsBuilder<ResumeDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
    }

    public ResumeDbContext CreateDbContext() =>
        _options is null ? new ResumeDbContext() : new ResumeDbContext(_options);

    /// <summary>Creates the factory for the app's real database and ensures the schema is current.</summary>
    public static ResumeDbContextFactory CreateInitialized()
    {
        var factory = new ResumeDbContextFactory();
        using var context = factory.CreateDbContext();
        DatabaseInitializer.Initialize(context);
        return factory;
    }
}
