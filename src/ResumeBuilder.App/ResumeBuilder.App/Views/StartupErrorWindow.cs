using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ResumeBuilder.Data;

namespace ResumeBuilder.App.Views;

/// <summary>
/// The window shown instead of the editor when the database cannot be opened. Built in code
/// because it must not depend on anything that could itself fail to load; its whole job is to
/// name the file and the backups next to it, then let the user close the app.
/// </summary>
public sealed class StartupErrorWindow : Window
{
    private StartupErrorWindow()
    {
        Title = "Resume Builder could not open your database";
        Width = 640;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    public static StartupErrorWindow For(DatabaseOpenException error)
    {
        var window = new StartupErrorWindow();
        var backups = error.AvailableBackups;

        var lines = new StackPanel { Margin = new Thickness(24), Spacing = 12 };
        lines.Children.Add(Paragraph(error.Message, bold: true));
        lines.Children.Add(Paragraph(
            "Nothing has been changed. The file is still where it was, and the app will not touch it " +
            "until it can be opened."));

        if (backups.Count > 0)
        {
            lines.Children.Add(Paragraph(
                "Backups taken before earlier upgrades are next to it, newest first. Move the broken " +
                "file aside and rename the newest backup to resumes.db to go back to that point:"));
            foreach (var backup in backups)
                lines.Children.Add(Paragraph(backup, monospace: true));
        }
        else
        {
            lines.Children.Add(Paragraph(
                "There are no backups next to it. Move the file aside and the app will start with an " +
                "empty database; keep the moved file in case its data can be recovered."));
        }

        var close = new Button
        {
            Content = "Close",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 96,
        };
        close.Click += (_, _) => window.Close();
        lines.Children.Add(close);

        window.Content = lines;
        return window;
    }

    private static TextBlock Paragraph(string text, bool bold = false, bool monospace = false) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
        FontFamily = monospace ? new FontFamily("Cascadia Mono,Consolas,monospace") : FontFamily.Default,
    };
}
