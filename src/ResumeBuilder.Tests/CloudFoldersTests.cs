using AwesomeAssertions;
using ResumeBuilder.Core.Sync;

namespace ResumeBuilder.Tests;

/// <summary>
/// Detection is machine-dependent, so these pin the invariants that hold on any machine rather
/// than asserting a particular provider is installed.
/// </summary>
public class CloudFoldersTests
{
    [Fact]
    public void Detect_OnlyReturnsFoldersThatExist()
    {
        // Offering a path that is not there sends the user to a folder picker that opens nowhere.
        foreach (var folder in CloudFolders.Detect())
            Directory.Exists(folder.Path).Should().BeTrue($"{folder.Name} was offered at {folder.Path}");
    }

    [Fact]
    public void Detect_DoesNotRepeatAFolder()
    {
        // OneDrive matches both its environment variable and the profile path; Drive for desktop
        // can match a mounted letter and a profile path. The same folder must be offered once.
        var paths = CloudFolders.Detect().Select(f => f.Path.ToLowerInvariant()).ToList();

        paths.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Detect_NamesEveryFolder()
    {
        foreach (var folder in CloudFolders.Detect())
            folder.Name.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SuggestSyncPath_LandsInASubfolderNotTheCloudRoot()
    {
        var folder = new CloudFolder("Google Drive", Path.Combine("C:", "Users", "x", "My Drive"));

        var suggested = CloudFolders.SuggestSyncPath(folder);

        // Syncing to the root of someone's Drive would scatter .json files among their documents.
        suggested.Should().NotBe(folder.Path);
        suggested.Should().StartWith(folder.Path);
        suggested.Should().EndWith("Resume Builder");
    }
}
