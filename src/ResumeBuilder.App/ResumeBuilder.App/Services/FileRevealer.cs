using System;
using System.Diagnostics;
using System.IO;

namespace ResumeBuilder.App.Services;

/// <summary>
/// Opens an exported file, or its folder with the file selected, in the OS shell. Lets the
/// post-export toast close the "where did it go?" loop with one click.
/// </summary>
public static class FileRevealer
{
    public static void Open(string filePath)
    {
        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    public static void RevealInFolder(string filePath)
    {
        if (OperatingSystem.IsWindows())
        {
            // explorer's /select wants the comma glued to the quoted path.
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start("open", $"-R \"{filePath}\"");
        }
        else
        {
            // No cross-desktop "select file" on Linux; opening the folder is the portable answer.
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
        }
    }
}
