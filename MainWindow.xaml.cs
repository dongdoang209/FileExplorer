using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FileExplorer
{
    public partial class MainWindow : System.Windows.Window
    {
        private readonly List<TabPage> _tabs = new();
        private int _currentTabIndex = 0;
        private AppSettings _settings = new();

        public MainWindow()
        {
            InitializeComponent();
            
            LoadSettings();
            ApplyTheme(_settings.Theme);
            
            // 创建第一个标签
            CreateNewTab();
        }

        private void LoadSettings()
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
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                _settings = new AppSettings();
            }
        }

        private TabPage CreateNewTab(string? path = null)
        {
            var tabPage = new TabPage();
            tabPage.LoadSettings(_settings);
            
            var appResources = System.Windows.Application.Current.Resources;
            
            // 添加标签按钮
            var tabButton = new System.Windows.Controls.Button
            {
                Content = tabPage.TabName,
                Style = appResources["TabButton"] as System.Windows.Style,
                Tag = _tabs.Count
            };
            tabButton.Click += TabButton_Click;
            
            // 添加关闭按钮
            var closeButton = new System.Windows.Controls.Button
            {
                Content = "×",
                Style = appResources["TabButton"] as System.Windows.Style,
                Tag = _tabs.Count,
                FontSize = 12,
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(2, 0, 0, 0)
            };
            closeButton.Click += CloseTab_Click;
            
            var tabContainer = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Tag = _tabs.Count };
            tabContainer.Children.Add(tabButton);
            tabContainer.Children.Add(closeButton);
            
            TabBar.Items.Add(tabContainer);
            
            // 添加到列表
            _tabs.Add(tabPage);
            tabPage.PathChanged += TabPage_PathChanged;
            tabPage.OpenInNewTabRequested += TabPage_OpenInNewTabRequested;
            
            // 如果有默认路径或设置了默认路径
            var startPath = path ?? (!string.IsNullOrEmpty(_settings.DefaultPath) && Directory.Exists(_settings.DefaultPath)
                ? _settings.DefaultPath
                : Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            
            if (_tabs.Count == 1)
            {
                // 第一个标签直接显示
                TabContentArea.Children.Clear();
                TabContentArea.Children.Add(tabPage);
                tabPage.NavigateTo(startPath);
                UpdateTabHighlight(0);
            }
            else
            {
                tabPage.Visibility = Visibility.Collapsed;
                TabContentArea.Children.Add(tabPage);
                tabPage.NavigateTo(startPath);
            }
            
            return tabPage;
        }

        private void TabPage_PathChanged(object? sender, string path)
        {
            if (sender is TabPage tab)
            {
                var index = _tabs.IndexOf(tab);
                UpdateTabName(index, System.IO.Path.GetFileName(path) ?? path);
            }
        }

        private void TabPage_OpenInNewTabRequested(object? sender, string path)
        {
            // 创建新标签并导航到指定路径
            var newTab = CreateNewTab(path);
            SwitchToTab(_tabs.Count - 1);
        }

        private void UpdateTabName(int index, string name)
        {
            if (index < TabBar.Items.Count)
            {
                var container = TabBar.Items[index] as StackPanel;
                if (container != null && container.Children.Count > 0 && container.Children[0] is System.Windows.Controls.Button btn)
                {
                    btn.Content = name;
                }
            }
        }

        private void UpdateTabHighlight(int index)
        {
            var bgPrimary = System.Windows.Application.Current.Resources["BgPrimary"] as System.Windows.Media.Brush;
            var bgToolbar = System.Windows.Application.Current.Resources["BgToolbar"] as System.Windows.Media.Brush;
            
            for (int i = 0; i < TabBar.Items.Count; i++)
            {
                var container = TabBar.Items[i] as StackPanel;
                if (container != null && container.Children.Count > 0 && container.Children[0] is System.Windows.Controls.Button btn)
                {
                    btn.Background = (i == index) ? bgPrimary : bgToolbar;
                }
            }
        }

        private void SwitchToTab(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;
            
            // 隐藏当前标签
            _tabs[_currentTabIndex].Visibility = Visibility.Collapsed;
            
            // 显示新标签
            _currentTabIndex = index;
            _tabs[index].Visibility = Visibility.Visible;
            
            // 更新路径显示
            TxtPath.Text = _tabs[index].CurrentPath;
            
            // 更新标签高亮
            UpdateTabHighlight(index);
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int index)
            {
                SwitchToTab(index);
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int index)
            {
                CloseTab(index);
            }
        }

        private void CloseTab(int index)
        {
            if (_tabs.Count <= 1) return; // 至少保留一个标签
            
            // 移除标签页
            var tab = _tabs[index];
            tab.PathChanged -= TabPage_PathChanged;
            _tabs.RemoveAt(index);
            TabBar.Items.RemoveAt(index);
            
            // 从UI移除
            if (TabContentArea.Children.Contains(tab))
                TabContentArea.Children.Remove(tab);
            
            // 更新所有标签的索引
            for (int i = 0; i < TabBar.Items.Count; i++)
            {
                var container = TabBar.Items[i] as StackPanel;
                if (container != null)
                {
                    container.Tag = i;
                    foreach (var child in container.Children)
                    {
                        if (child is System.Windows.Controls.Button b) b.Tag = i;
                    }
                }
            }
            
            // 如果关闭的是当前标签
            if (index == _currentTabIndex)
            {
                if (_currentTabIndex >= _tabs.Count)
                    _currentTabIndex = _tabs.Count - 1;
                SwitchToTab(_currentTabIndex);
            }
            else if (index < _currentTabIndex)
            {
                _currentTabIndex--;
                UpdateTabHighlight(_currentTabIndex);
            }
        }

        private void BtnNewTab_Click(object sender, RoutedEventArgs e)
        {
            CreateNewTab();
            SwitchToTab(_tabs.Count - 1);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            _tabs[_currentTabIndex].GoBack();
            TxtPath.Text = _tabs[_currentTabIndex].CurrentPath;
        }

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            _tabs[_currentTabIndex].GoForward();
            TxtPath.Text = _tabs[_currentTabIndex].CurrentPath;
        }

        private void BtnUp_Click(object sender, RoutedEventArgs e)
        {
            _tabs[_currentTabIndex].GoUp();
            TxtPath.Text = _tabs[_currentTabIndex].CurrentPath;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _tabs[_currentTabIndex].Refresh();
        }

        private void BtnGo_Click(object sender, RoutedEventArgs e)
        {
            _tabs[_currentTabIndex].NavigateTo(TxtPath.Text);
        }

        private void TxtPath_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _tabs[_currentTabIndex].NavigateTo(TxtPath.Text);
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            // 视图切换功能
        }

        private void TxtEverythingSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OpenEverythingSearch();
            }
        }

        private void BtnEverythingSearch_Click(object sender, RoutedEventArgs e)
        {
            OpenEverythingSearch();
        }

        private void OpenEverythingSearch()
        {
            var keyword = TxtEverythingSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                System.Windows.MessageBox.Show("请输入搜索关键词", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var everythingPath = "D:\\FileExplorer\\bin\\Everything\\everything.exe";
                
                if (!System.IO.File.Exists(everythingPath))
                {
                    System.Windows.MessageBox.Show($"Everything 未找到:\n{everythingPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 直接打开 Everything 并搜索
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = everythingPath,
                    Arguments = $"-search \"{keyword}\"",
                    UseShellExecute = true
                });
                
                StatusText.Text = $"🔍 已在 Everything 中搜索: {keyword}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"搜索失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
                LoadSettings();
                ApplyTheme(_settings.Theme);
                
                // 更新所有标签的设置
                foreach (var tab in _tabs)
                {
                    tab.LoadSettings(_settings);
                    tab.Refresh();
                }
            }
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