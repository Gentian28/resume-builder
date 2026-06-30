using Avalonia;
using Avalonia.Styling;
using System;
using System.IO;
using System.Text.Json;

namespace ResumeBuilder.App.Services;

public class ThemeService
{
    private const string SettingsFileName = "settings.json";
    private readonly string _settingsPath;

    public ThemeVariant CurrentTheme { get; private set; } = ThemeVariant.Default;
    public event Action<ThemeVariant>? ThemeChanged;

    public ThemeService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ResumeBuilder");

        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, SettingsFileName);

        LoadThemePreference();
    }

    public void SetTheme(ThemeVariant theme)
    {
        CurrentTheme = theme;

        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = theme;
        }

        SaveThemePreference();
        ThemeChanged?.Invoke(theme);
    }

    public void ToggleTheme()
    {
        var newTheme = CurrentTheme == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;

        SetTheme(newTheme);
    }

    public void SetLightTheme() => SetTheme(ThemeVariant.Light);
    public void SetDarkTheme() => SetTheme(ThemeVariant.Dark);
    public void SetSystemTheme() => SetTheme(ThemeVariant.Default);

    public bool IsDarkTheme => CurrentTheme == ThemeVariant.Dark ||
        (CurrentTheme == ThemeVariant.Default && IsSystemDarkMode());

    private static bool IsSystemDarkMode()
    {
        return Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
    }

    private void LoadThemePreference()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings != null)
                {
                    CurrentTheme = settings.Theme switch
                    {
                        "Light" => ThemeVariant.Light,
                        "Dark" => ThemeVariant.Dark,
                        _ => ThemeVariant.Default
                    };
                }
            }
        }
        catch
        {
            CurrentTheme = ThemeVariant.Default;
        }
    }

    private void SaveThemePreference()
    {
        try
        {
            var settings = new AppSettings
            {
                Theme = CurrentTheme == ThemeVariant.Light ? "Light" :
                        CurrentTheme == ThemeVariant.Dark ? "Dark" : "System"
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    private class AppSettings
    {
        public string Theme { get; set; } = "System";
    }
}
