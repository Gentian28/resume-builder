using Microsoft.EntityFrameworkCore;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Data;

public class ResumeDbContext : DbContext
{
    public DbSet<Resume> Resumes => Set<Resume>();

    private readonly string _dbPath;

    public ResumeDbContext()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ResumeBuilder");

        Directory.CreateDirectory(appDataPath);
        _dbPath = Path.Combine(appDataPath, "resumes.db");
    }

    public ResumeDbContext(DbContextOptions<ResumeDbContext> options) : base(options)
    {
        _dbPath = "resumes.db";
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
            entity.Property(r => r.Summary).HasMaxLength(5000);

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

            // Store collections as JSON
            entity.Property(r => r.Experiences)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Experience>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Experience>());

            entity.Property(r => r.EducationList)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Education>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Education>());

            entity.Property(r => r.Skills)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Skill>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Skill>());

            entity.Property(r => r.Languages)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Language>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Language>());

            entity.Property(r => r.Certifications)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Certification>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Certification>());

            entity.Property(r => r.Projects)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Project>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Project>());

            entity.Property(r => r.CustomSections)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<CustomSection>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<CustomSection>());

            entity.Property(r => r.TemplateSettings)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<TemplateSettings>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new TemplateSettings());

            entity.Property(r => r.SectionOrder)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<SectionOrder>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new SectionOrder());
        });
    }
}
