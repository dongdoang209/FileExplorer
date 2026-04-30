using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace FileExplorer
{
    public partial class App : System.Windows.Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            LoadThemeFromSettings();
        }

        private void LoadThemeFromSettings()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FileExplorer", "settings.json"
                );
                
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        ApplyTheme(settings.Theme);
                    }
                }
            }
            catch { }
        }

        public static void ApplyTheme(string theme)
        {
            var resources = System.Windows.Application.Current.Resources;
            
            if (theme == "light")
            {
                resources["BgPrimary"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                resources["BgSecondary"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["BgToolbar"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
                resources["BgHover"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));
                resources["BgSelected"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 210, 240));
                resources["FgPrimary"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                resources["FgSecondary"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));
                resources["FgAccent"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                resources["BorderColor"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
                resources["StatusBar"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
            }
            else
            {
                resources["BgPrimary"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
                resources["BgSecondary"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 37, 38));
                resources["BgToolbar"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48));
                resources["BgHover"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                resources["BgSelected"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 79, 120));
                resources["FgPrimary"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["FgSecondary"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136));
                resources["FgAccent"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));
                resources["BorderColor"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 85, 85));
                resources["StatusBar"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204));
            }
        }
    }
}