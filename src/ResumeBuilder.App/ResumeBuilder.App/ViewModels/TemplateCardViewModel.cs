using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ResumeBuilder.App.Services;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.App.ViewModels;

/// <summary>
/// One card in the template gallery: the template's own metadata plus its rendered preview.
///
/// The preview lives here rather than on <see cref="TemplateInfo"/> because that is a Core model,
/// and Core is kept free of UI types so it stays usable server-side (see docs/web-app-plan.md).
/// </summary>
public partial class TemplateCardViewModel : ObservableObject
{
    private readonly TemplateThumbnailService _thumbnails;

    public TemplateInfo Info { get; }

    [ObservableProperty]
    private Bitmap? _thumbnail;

    /// <summary>
    /// Drives a placeholder so a card never renders as an empty hole while its preview is being
    /// generated. Also false when rendering failed, in which case Thumbnail stays null and the
    /// card falls back to text — a template you cannot preview is still a template you can pick.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingThumbnail = true;

    public TemplateCardViewModel(TemplateInfo info, TemplateThumbnailService thumbnails)
    {
        Info = info;
        _thumbnails = thumbnails;
    }

    public async Task LoadThumbnailAsync(CancellationToken cancellationToken = default)
    {
        if (Thumbnail is not null)
        {
            IsLoadingThumbnail = false;
            return;
        }

        Thumbnail = await _thumbnails.GetAsync(Info.Id, cancellationToken).ConfigureAwait(true);
        IsLoadingThumbnail = false;
    }
}
