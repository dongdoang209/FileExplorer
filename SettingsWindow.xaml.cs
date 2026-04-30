using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace FileExplorer
{
    public partial class SettingsWindow : System.Windows.Window
    {
        private readonly string _settingsPath;
        private AppSettings _settings;

        public AppSettings Settings => _settings;

        public SettingsWindow()
        {
            InitializeComponent();
            
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FileExplorer"
            );
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");

            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _settings = new AppSettings();
                }

                TxtDefaultPath.Text = _settings.DefaultPath;
                ChkShowHidden.IsChecked = _settings.ShowHiddenFiles;
                ChkShowExtensions.IsChecked = _settings.ShowExtensions;

                // 主题
                int themeIndex = _settings.Theme == "light" ? 1 : 0;
                CmbTheme.SelectedIndex = themeIndex;

                // 排序
                var sortValues = new[] { "name_asc", "name_desc", "size_asc", "size_desc", "date_desc", "date_asc" };
                for (int i = 0; i < sortValues.Length; i++)
                {
                    if (_settings.SortBy == sortValues[i])
                    {
                        CmbSortBy.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch
            {
                _settings = new AppSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                // 获取主题
                _settings.Theme = CmbTheme.SelectedIndex == 1 ? "light" : "dark";

                // 获取其他设置
                _settings.DefaultPath = TxtDefaultPath.Text;
                _settings.ShowHiddenFiles = ChkShowHidden.IsChecked == true;
                _settings.ShowExtensions = ChkShowExtensions.IsChecked == true;

                // 排序
                var sortValues = new[] { "name_asc", "name_desc", "size_asc", "size_desc", "date_desc", "date_asc" };
                _settings.SortBy = sortValues[CmbSortBy.SelectedIndex];

                // 保存到 JSON
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsPath, json);

                // 立即应用主题
                App.ApplyTheme(_settings.Theme);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存设置失败: {ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择默认路径",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtDefaultPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}