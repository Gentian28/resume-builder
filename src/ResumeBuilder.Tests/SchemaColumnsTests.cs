using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ResumeBuilder.Data;

namespace ResumeBuilder.Tests;

/// <summary>
/// There are no EF migrations. A property added to a model reaches a fresh install through
/// EnsureCreated and reaches nobody else unless DatabaseInitializer.AddedColumns names it, and the
/// difference only shows on a user's machine as a missing-column error. This test freezes the
/// columns each table had when its schema was first shipped; anything the model has beyond that
/// must be in AddedColumns.
/// </summary>
public class SchemaColumnsTests
{
    // PersonalInfo is an owned type sharing the owner's table; EF prefixes its columns and gives
    // its key a column of its own.
    private static readonly string[] PersonalInfoColumns =
    {
        "PersonalInfo_Id", "PersonalInfo_FirstName", "PersonalInfo_LastName", "PersonalInfo_JobTitle",
        "PersonalInfo_Email", "PersonalInfo_Phone", "PersonalInfo_Address", "PersonalInfo_City",
        "PersonalInfo_Country", "PersonalInfo_PostalCode", "PersonalInfo_Website", "PersonalInfo_LinkedIn",
        "PersonalInfo_GitHub", "PersonalInfo_Photo",
    };

    private static readonly Dictionary<string, string[]> FirstShippedColumns = new()
    {
        ["Resumes"] = new[]
        {
            "Id", "Name", "CreatedAt", "UpdatedAt", "SelectedTemplateId", "AccentColor", "FontFamily",
            "TemplateSettings", "SectionOrder", "Summary", "Experiences", "EducationList", "Skills",
            "Languages", "Certifications", "Projects", "CustomSections",
        }.Concat(PersonalInfoColumns).ToArray(),
        ["CoverLetters"] = new[]
        {
            "Id", "SyncId", "RowVersion", "Name", "CreatedAt", "UpdatedAt", "ResumeId", "SelectedTemplateId",
            "TemplateSettings", "RecipientName", "RecipientTitle", "CompanyName", "CompanyAddress", "LetterDate",
            "Subject", "Salutation", "Paragraphs", "Closing", "JobDescription",
        }.Concat(PersonalInfoColumns).ToArray(),
        ["JobApplications"] = new[]
        {
            "Id", "ResumeId", "Company", "Role", "Status", "AppliedOn", "Link", "Notes", "CreatedAt", "UpdatedAt",
        },
    };

    private static Dictionary<string, List<string>> ModelColumnsByTable(ResumeDbContext context) =>
        context.Model.GetEntityTypes()
            .Where(e => e.GetTableName() is not null)
            .GroupBy(e => e.GetTableName()!)
            .ToDictionary(g => g.Key, g =>
            {
                var table = StoreObjectIdentifier.Table(g.Key);
                return g.SelectMany(e => e.GetProperties())
                    .Select(p => p.GetColumnName(table))
                    .Where(c => c is not null)
                    .Select(c => c!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            });

    private static ResumeDbContext InMemory() => new(new DbContextOptionsBuilder<ResumeDbContext>()
        .UseSqlite("Data Source=:memory:").Options);

    [Fact]
    public void EveryModelColumn_IsEitherFirstShipped_OrListedInAddedColumns()
    {
        using var context = InMemory();
        var added = DatabaseInitializer.ColumnsAddedSinceFirstRelease;
        var byTable = ModelColumnsByTable(context);

        byTable.Keys.Should().BeEquivalentTo(FirstShippedColumns.Keys, "a new table needs its own frozen column list here");

        foreach (var (table, columns) in byTable)
        {
            var known = FirstShippedColumns[table]
                .Concat(added.TryGetValue(table, out var extra) ? extra : Array.Empty<string>())
                .ToList();

            columns.Should().BeEquivalentTo(known,
                $"every column the model maps onto {table} must be first-shipped or in DatabaseInitializer.AddedColumns, or existing installs never get it");
        }
    }

    [Fact]
    public void AddedColumns_NameOnlyTablesAndColumnsTheModelHas()
    {
        using var context = InMemory();
        var byTable = ModelColumnsByTable(context);

        foreach (var (table, columns) in DatabaseInitializer.ColumnsAddedSinceFirstRelease)
        {
            byTable.Should().ContainKey(table, $"AddedColumns names a table {table} the model does not map");
            columns.Should().BeSubsetOf(byTable[table], $"AddedColumns lists a column {table} no longer has");
        }
    }

    [Fact]
    public void TheCreateScript_ProducesExactlyTheModelColumns()
    {
        // The initializer creates missing tables from this script and adds missing columns from
        // AddedColumns; a fresh install and an upgraded one must end up with the same columns.
        using var context = InMemory();
        context.Database.OpenConnection();
        DatabaseInitializer.Initialize(context);

        foreach (var (table, columns) in ModelColumnsByTable(context))
        {
            var actual = new List<string>();
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{table}\")";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                actual.Add(reader.GetString(1));

            actual.Should().BeEquivalentTo(columns, $"the database and the model disagree about {table}");
        }
    }
}
