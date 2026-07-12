using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Data;

public class ResumeDbContext : DbContext
{
    /// <summary>Kept in step with <c>ResumeValidator</c>'s summary limit.</summary>
    public const int SummaryMaxLength = 5000;

    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<CoverLetter> CoverLetters => Set<CoverLetter>();

    private readonly string _dbPath;

    public ResumeDbContext()
    {
        _dbPath = DefaultDatabasePath();
    }

    public ResumeDbContext(DbContextOptions<ResumeDbContext> options) : base(options)
    {
        _dbPath = "resumes.db";
    }

    public static string DefaultDatabasePath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ResumeBuilder");

        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "resumes.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Resume>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
            entity.Property(r => r.SelectedTemplateId).HasMaxLength(50);
            entity.Property(r => r.AccentColor).HasMaxLength(20);
            entity.Property(r => r.FontFamily).HasMaxLength(100);
            entity.Property(r => r.Summary).HasMaxLength(SummaryMaxLength);

            // Optimistic concurrency: the repository rotates this on every update and matches on
            // the original value, so a stale writer fails loudly instead of clobbering.
            entity.Property(r => r.RowVersion).IsConcurrencyToken();

            // Store PersonalInfo as owned entity
            entity.OwnsOne(r => r.PersonalInfo, pi =>
            {
                pi.Property(p => p.FirstName).HasMaxLength(100);
                pi.Property(p => p.LastName).HasMaxLength(100);
                pi.Property(p => p.JobTitle).HasMaxLength(200);
                pi.Property(p => p.Email).HasMaxLength(200);
                pi.Property(p => p.Phone).HasMaxLength(50);
                pi.Property(p => p.Address).HasMaxLength(300);
                pi.Property(p => p.City).HasMaxLength(100);
                pi.Property(p => p.Country).HasMaxLength(100);
                pi.Property(p => p.PostalCode).HasMaxLength(20);
                pi.Property(p => p.Website).HasMaxLength(300);
                pi.Property(p => p.LinkedIn).HasMaxLength(300);
                pi.Property(p => p.GitHub).HasMaxLength(300);
            });

            // Collections and settings objects are stored as JSON columns. Each needs a
            // ValueComparer, otherwise EF compares by reference and never notices in-place edits
            // (list.Add(...), settings.AccentColor = ...) — those changes would silently not save.
            entity.JsonList(r => r.Experiences);
            entity.JsonList(r => r.EducationList);
            entity.JsonList(r => r.Skills);
            entity.JsonList(r => r.Languages);
            entity.JsonList(r => r.Certifications);
            entity.JsonList(r => r.Projects);
            entity.JsonList(r => r.CustomSections);
            entity.JsonObject(r => r.TemplateSettings);
            entity.JsonObject(r => r.SectionOrder);

            entity.Property(r => r.TargetRole).HasMaxLength(300);
            entity.HasIndex(r => r.BaseResumeId);
        });

        modelBuilder.Entity<CoverLetter>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.SelectedTemplateId).HasMaxLength(50);
            entity.Property(c => c.RecipientName).HasMaxLength(200);
            entity.Property(c => c.RecipientTitle).HasMaxLength(200);
            entity.Property(c => c.CompanyName).HasMaxLength(200);
            entity.Property(c => c.CompanyAddress).HasMaxLength(500);
            entity.Property(c => c.Subject).HasMaxLength(300);
            entity.Property(c => c.Salutation).HasMaxLength(200);
            entity.Property(c => c.Closing).HasMaxLength(100);
            entity.Property(c => c.RowVersion).IsConcurrencyToken();

            entity.OwnsOne(c => c.PersonalInfo);

            entity.JsonList(c => c.Paragraphs);
            entity.JsonObject(c => c.TemplateSettings);

            entity.HasIndex(c => c.ResumeId);
        });
    }
}

internal static class JsonPropertyBuilderExtensions
{
    private static readonly JsonSerializerOptions Options = new();

    public static void JsonList<TEntity, TItem>(
        this EntityTypeBuilder<TEntity> entity,
        Expression<Func<TEntity, List<TItem>>> property)
        where TEntity : class
    {
        entity.Property(property)
            .HasConversion(
                v => JsonSerializer.Serialize(v, Options),
                v => JsonSerializer.Deserialize<List<TItem>>(v, Options) ?? new List<TItem>(),
                new ValueComparer<List<TItem>>(
                    (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
                    v => JsonSerializer.Serialize(v, Options).GetHashCode(),
                    v => JsonSerializer.Deserialize<List<TItem>>(JsonSerializer.Serialize(v, Options), Options)!));
    }

    public static void JsonObject<TEntity, TValue>(
        this EntityTypeBuilder<TEntity> entity,
        Expression<Func<TEntity, TValue>> property)
        where TEntity : class
        where TValue : class, new()
    {
        entity.Property(property)
            .HasConversion(
                v => JsonSerializer.Serialize(v, Options),
                v => JsonSerializer.Deserialize<TValue>(v, Options) ?? new TValue(),
                new ValueComparer<TValue>(
                    (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
                    v => JsonSerializer.Serialize(v, Options).GetHashCode(),
                    v => JsonSerializer.Deserialize<TValue>(JsonSerializer.Serialize(v, Options), Options)!));
    }
}
