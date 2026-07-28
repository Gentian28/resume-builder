using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace ResumeBuilder.App.Services;

/// <summary>
/// Checks for and applies Velopack updates.
///
/// Lives in the App layer, not Core: Velopack is a desktop-install concern and Core/Templates/Export
/// are kept server-compatible for the future web version (see docs/web-app-plan.md).
///
/// Degrades rather than gates, like the AI features. Running from source, from the portable zip, or
/// from any build that was not installed by Velopack leaves <see cref="IsSupported"/> false and every
/// operation a no-op — there is no install to update, and pretending otherwise would surface errors
/// to users who did nothing wrong.
/// </summary>
public sealed class UpdateService
{
    private readonly UpdateManager? _manager;
    private UpdateInfo? _pending;

    /// <summary>
    /// The GitHub releases feed doubles as the update feed, so updates work before any
    /// separate hosting exists. Point this at an R2 URL later by changing the source only.
    /// </summary>
    public const string DefaultFeedUrl = "https://github.com/Gentian28/resumebuilder";

    public UpdateService(string? feedUrl = null)
    {
        try
        {
            var url = string.IsNullOrWhiteSpace(feedUrl) ? DefaultFeedUrl : feedUrl;
            _manager = new UpdateManager(new GithubSource(url, accessToken: null, prerelease: false));
        }
        catch (Exception)
        {
            // A malformed feed URL must not stop the app from starting.
            _manager = null;
        }
    }

    /// <summary>True only when this build was installed by Velopack and can actually be updated.</summary>
    public bool IsSupported => _manager?.IsInstalled == true;

    /// <summary>The version currently running, or null when not running from an install.</summary>
    public string? CurrentVersion => _manager?.CurrentVersion?.ToString();

    /// <summary>Version available to install, set once <see cref="CheckAsync"/> finds one.</summary>
    public string? AvailableVersion => _pending?.TargetFullRelease.Version.ToString();

    /// <summary>
    /// Returns true when a newer release is available. Never throws: an update check failing
    /// because the machine is offline is not something to interrupt the user over.
    /// </summary>
    public async Task<bool> CheckAsync()
    {
        if (!IsSupported)
            return false;

        try
        {
            _pending = await _manager!.CheckForUpdatesAsync().ConfigureAwait(false);
            return _pending is not null;
        }
        catch (Exception)
        {
            _pending = null;
            return false;
        }
    }

    /// <summary>
    /// Downloads the pending update in the background. Returns false if it could not complete,
    /// leaving the running install untouched.
    /// </summary>
    public async Task<bool> DownloadAsync()
    {
        if (!IsSupported || _pending is null)
            return false;

        try
        {
            await _manager!.DownloadUpdatesAsync(_pending).ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Applies the downloaded update and restarts. This exits the process, so callers must save
    /// first — an unsaved résumé lost to an update the user did not initiate would be inexcusable.
    /// </summary>
    public void ApplyAndRestart()
    {
        if (!IsSupported || _pending is null)
            return;

        _manager!.ApplyUpdatesAndRestart(_pending);
    }
}
