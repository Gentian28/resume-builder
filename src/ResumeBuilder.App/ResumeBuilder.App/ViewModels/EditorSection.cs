using CommunityToolkit.Mvvm.ComponentModel;

namespace ResumeBuilder.App.ViewModels;

/// <summary>
/// One entry in the editor's section navigation.
///
/// <see cref="Count"/> is shown beside the name so the list says where the content actually is
/// without the user opening each section to find out. It is null for sections that are not lists.
/// </summary>
public partial class EditorSection : ObservableObject
{
    /// <summary>Matches the ConverterParameter on the corresponding panel in MainWindow.axaml.</summary>
    public string Key { get; }

    public string Name { get; }

    /// <summary>Groups the list under "Content" and "Document" headings.</summary>
    public bool IsDocumentSetting { get; }

    [ObservableProperty]
    private int? _count;

    /// <summary>
    /// Drives the highlight. Held per item rather than compared in the view because a navigation
    /// list with no visible current position is worse than no navigation at all.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    public EditorSection(string key, string name, bool isDocumentSetting = false)
    {
        Key = key;
        Name = name;
        IsDocumentSetting = isDocumentSetting;
    }
}
