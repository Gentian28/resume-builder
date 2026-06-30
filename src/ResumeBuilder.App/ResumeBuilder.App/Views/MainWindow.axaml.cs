using Avalonia.Controls;
using Avalonia.Interactivity;
using ResumeBuilder.App.ViewModels;

namespace ResumeBuilder.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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

    private void CloseResumeList_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowResumeList = false;
        }
    }

    private void ToggleAiPanel_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowAiPanel = !vm.ShowAiPanel;
        }
    }

    private void ToggleKeywordPanel_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowKeywordPanel = !vm.ShowKeywordPanel;
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