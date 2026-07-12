using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace ResumeBuilder.App.Services;

public enum UnsavedChangesChoice
{
    Save,
    Discard,
    Cancel
}

/// <summary>
/// Modal dialogs built in code so failures and destructive actions are impossible to miss.
/// The status bar is left for transient success messages.
/// </summary>
public static class DialogService
{
    private static Window? Owner =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public static async Task ShowErrorAsync(string title, string message)
    {
        await ShowChoiceAsync(title, message, isError: true, "OK");
    }

    public static async Task<bool> ConfirmAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        var choice = await ShowChoiceAsync(title, message, isError: false, confirmText, cancelText);
        return choice == 0;
    }

    public static async Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync(string resumeName)
    {
        var choice = await ShowChoiceAsync(
            "Unsaved changes",
            $"\"{resumeName}\" has unsaved changes. Save them before continuing?",
            isError: false,
            "Save", "Discard", "Cancel");

        return choice switch
        {
            0 => UnsavedChangesChoice.Save,
            1 => UnsavedChangesChoice.Discard,
            _ => UnsavedChangesChoice.Cancel
        };
    }

    /// <summary>Returns the entered text, or null when the user cancels.</summary>
    public static async Task<string?> PromptAsync(string title, string message, string initialValue = "")
    {
        var owner = Owner;
        if (owner == null) return null;

        string? result = null;

        var input = new TextBox { Text = initialValue, Watermark = "Name" };
        var dialog = CreateDialog(title);

        var okButton = new Button { Content = "OK", IsDefault = true, MinWidth = 80 };
        var cancelButton = new Button { Content = "Cancel", IsCancel = true, MinWidth = 80 };

        okButton.Click += (_, _) =>
        {
            result = string.IsNullOrWhiteSpace(input.Text) ? null : input.Text.Trim();
            dialog.Close();
        };
        cancelButton.Click += (_, _) => dialog.Close();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(cancelButton);
        buttons.Children.Add(okButton);

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(input);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        input.SelectAll();
        await dialog.ShowDialog(owner);
        return result;
    }

    /// <summary>Returns the index of the chosen option, or -1 when the dialog is dismissed.</summary>
    private static async Task<int> ShowChoiceAsync(string title, string message, bool isError, params string[] options)
    {
        var owner = Owner;
        if (owner == null) return -1;

        var chosen = -1;
        var dialog = CreateDialog(title);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        for (var i = 0; i < options.Length; i++)
        {
            var index = i;
            var button = new Button
            {
                Content = options[i],
                MinWidth = 80,
                IsDefault = i == 0,
                IsCancel = i == options.Length - 1 && options.Length > 1
            };
            button.Click += (_, _) =>
            {
                chosen = index;
                dialog.Close();
            };
            buttons.Children.Add(button);
        }

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 16 };

        if (isError)
        {
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.OrangeRed
            });
        }

        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(buttons);
        dialog.Content = panel;

        await dialog.ShowDialog(owner);
        return chosen;
    }

    private static Window CreateDialog(string title) => new()
    {
        Title = title,
        Width = 440,
        SizeToContent = SizeToContent.Height,
        CanResize = false,
        ShowInTaskbar = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner
    };
}
