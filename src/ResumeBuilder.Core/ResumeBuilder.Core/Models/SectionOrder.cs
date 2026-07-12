namespace ResumeBuilder.Core.Models;

public enum SectionType
{
    PersonalInfo,
    Summary,
    Experience,
    Education,
    Skills,
    Languages,
    Certifications,
    Projects,
    CustomSections
}

public class SectionOrder
{
    /// <summary>Every section in its canonical order — the seed for new resumes and the source for repair.</summary>
    public static readonly IReadOnlyList<SectionType> AllSections = new[]
    {
        SectionType.PersonalInfo,
        SectionType.Summary,
        SectionType.Experience,
        SectionType.Education,
        SectionType.Skills,
        SectionType.Languages,
        SectionType.Certifications,
        SectionType.Projects,
        SectionType.CustomSections
    };

    public List<SectionType> OrderedSections { get; set; } = AllSections.ToList();

    public Dictionary<SectionType, bool> Visibility { get; set; } =
        AllSections.ToDictionary(s => s, _ => true);

    public bool IsSectionVisible(SectionType section)
    {
        return Visibility.TryGetValue(section, out var visible) && visible;
    }

    public void SetSectionVisibility(SectionType section, bool visible)
    {
        Visibility[section] = visible;
    }

    public void MoveSection(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= OrderedSections.Count ||
            toIndex < 0 || toIndex >= OrderedSections.Count)
            return;

        var section = OrderedSections[fromIndex];
        OrderedSections.RemoveAt(fromIndex);
        OrderedSections.Insert(toIndex, section);
    }

    /// <summary>
    /// Adds any section missing from a persisted order (e.g. one introduced after the resume was
    /// saved) and drops duplicates, so older data still renders every section.
    /// </summary>
    public void EnsureAllSectionsPresent()
    {
        var seen = new HashSet<SectionType>();
        OrderedSections = OrderedSections.Where(s => Enum.IsDefined(s) && seen.Add(s)).ToList();

        foreach (var section in AllSections)
        {
            if (seen.Add(section))
            {
                OrderedSections.Add(section);
            }

            if (!Visibility.ContainsKey(section))
            {
                Visibility[section] = true;
            }
        }
    }

    public static SectionOrder Default => new();

    public static string GetSectionDisplayName(SectionType section) => section switch
    {
        SectionType.PersonalInfo => "Personal Information",
        SectionType.Summary => "Professional Summary",
        SectionType.Experience => "Work Experience",
        SectionType.Education => "Education",
        SectionType.Skills => "Skills",
        SectionType.Languages => "Languages",
        SectionType.Certifications => "Certifications",
        SectionType.Projects => "Projects",
        SectionType.CustomSections => "Custom Sections",
        _ => section.ToString()
    };
}
