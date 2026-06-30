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
    Projects
}

public class SectionOrder
{
    public List<SectionType> OrderedSections { get; set; } = new()
    {
        SectionType.PersonalInfo,
        SectionType.Summary,
        SectionType.Experience,
        SectionType.Education,
        SectionType.Skills,
        SectionType.Languages,
        SectionType.Certifications,
        SectionType.Projects
    };

    public Dictionary<SectionType, bool> Visibility { get; set; } = new()
    {
        { SectionType.PersonalInfo, true },
        { SectionType.Summary, true },
        { SectionType.Experience, true },
        { SectionType.Education, true },
        { SectionType.Skills, true },
        { SectionType.Languages, true },
        { SectionType.Certifications, true },
        { SectionType.Projects, true }
    };

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
        _ => section.ToString()
    };
}
