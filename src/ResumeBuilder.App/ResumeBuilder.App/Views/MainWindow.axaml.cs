using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
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
    /// Keeps the window inside the screen it opens on.
    ///
    /// The declared 1400x900 is in device-independent units, so on a display with display scaling
    /// the usable logical area can be far smaller than it looks: 1920x1080 at 150% is only
    /// 1280x720. The window was then larger than the screen and WindowStartupLocation="CenterScreen"
    /// centred the overflow, putting the title bar above the top edge — no close, no minimise, no
    /// way to drag it back, because MinWidth/MinHeight also blocked shrinking.
    ///
    /// Maximising rather than merely resizing is deliberate: if the preferred size does not fit,
    /// the user wants all the space there is, and a maximised window always has reachable chrome.
    ///
    /// MinWidth/MinHeight in the XAML were lowered to 800x500 for the same reason: a 1366x768
    /// laptop at 150% scaling has only 911x512 logical, so the previous 1000x600 floor meant the
    /// window could not be shrunk to fit even by hand.
    /// </summary>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
            return;

        // WorkingArea is physical pixels and excludes the taskbar; Width/Height are logical.
        var scaling = screen.Scaling <= 0 ? 1 : screen.Scaling;
        var availableWidth = screen.WorkingArea.Width / scaling;
        var availableHeight = screen.WorkingArea.Height / scaling;

        var tooWide = Width > availableWidth;
        var tooTall = Height > availableHeight;

        if (tooWide || tooTall)
        {
            // Shrink the window itself, not just its state. Maximising alone left Width/Height
            // at their declared values, so the restore button handed the user straight back the
            // off-screen window this is meant to prevent.
            Width = Math.Min(Width, Math.Floor(availableWidth * 0.9));
            Height = Math.Min(Height, Math.Floor(availableHeight * 0.9));
        }

        CentreOnWorkingArea(screen, scaling);

        if (tooWide || tooTall)
        {
            // Maximise on a later frame so the platform records the resized geometry above as the
            // restore rectangle. Doing it inline maximises before the new size has been applied,
            // and the restore rectangle keeps the old oversized bounds.
            Dispatcher.UIThread.Post(
                () => WindowState = WindowState.Maximized,
                DispatcherPriority.Background);
        }
    }

    private void CentreOnWorkingArea(Screen screen, double scaling)
    {
        Position = new PixelPoint(
            screen.WorkingArea.X + (int)((screen.WorkingArea.Width - Width * scaling) / 2),
            screen.WorkingArea.Y + (int)((screen.WorkingArea.Height - Height * scaling) / 2));
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





}
