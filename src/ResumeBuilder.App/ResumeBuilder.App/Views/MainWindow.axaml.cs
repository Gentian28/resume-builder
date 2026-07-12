using Avalonia.Controls;
using Avalonia.Interactivity;
using ResumeBuilder.App.ViewModels;

namespace ResumeBuilder.App.Views;

public partial class MainWindow : Window
{
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The prompt is asynchronous, so the first close is always cancelled and the window closes again
    /// once the user has chosen.
    /// </summary>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (!_closeConfirmed && DataContext is MainWindowViewModel vm && vm.IsDirty)
        {
            e.Cancel = true;

            if (await vm.ConfirmDiscardChangesAsync())
            {
                _closeConfirmed = true;
                Close();
            }

            return;
        }

        base.OnClosing(e);
    }

    private void ShowTemplateGallery_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowTemplateGallery = true;
        }
    }

    private void CloseTemplateGallery_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowTemplateGallery = false;
        }
    }

    private void ToggleAiPanel_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowAiPanel = !vm.ShowAiPanel;
        }
    }

    private void ToggleTailorPanel_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowTailorPanel = !vm.ShowTailorPanel;
        }
    }

    private void ToggleSyncPanel_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowSyncPanel = !vm.ShowSyncPanel;
        }
    }
}
