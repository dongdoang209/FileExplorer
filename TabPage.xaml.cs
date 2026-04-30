using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileExplorer
{
    public class FileItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string Icon { get; set; } = "📄";
        public string Info { get; set; } = "";
        public bool IsDirectory { get; set; }
    }

    public partial class TabPage : UserControl
    {
        private readonly ObservableCollection<FileItem> _files = new();
        private readonly Stack<string> _history = new();
        private readonly Stack<string> _forwardHistory = new();
        private string _currentPath = "";
        private AppSettings _settings = new();
        private bool _isThisPCView = false;
        
        public string CurrentPath => _currentPath;
        public string TabName { get; set; } = "此电脑";
        
        public event EventHandler<string>? PathChanged;

        public TabPage()
        {
            InitializeComponent();
            LoadDrives();
            // 启动时默认显示此电脑
            ShowThisPC();
        }

        public void LoadSettings(AppSettings settings)
        {
            _settings = settings;
        }

        // ===== 右键菜单事件处理 =====
        
        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileItem item)
            {
                if (item.IsDirectory)
                    NavigateTo(item.FullPath);
                else
                    OpenFile(item.FullPath);
            }
        }

        private void OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileItem item && item.IsDirectory)
            {
                // 通过事件通知MainWindow创建新标签
                OpenInNewTabRequested?.Invoke(this, item.FullPath);
            }
        }

        private void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("新建文件夹", "请输入文件夹名称:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    var newPath = System.IO.Path.Combine(_currentPath, dialog.InputText);
                    System.IO.Directory.CreateDirectory(newPath);
                    Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"创建失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileItem item)
            {
                _clipboard = item.FullPath;
                _isCutOperation = true;
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileItem item)
            {
                _clipboard = item.FullPath;
                _isCutOperation = false;
            }
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_clipboard) && !_isThisPCView)
            {
                try
                {
                    var destPath = System.IO.Path.Combine(_currentPath, System.IO.Path.GetFileName(_clipboard));
                    if (System.IO.Directory.Exists(_clipboard))
                    {
                        CopyDirectory(_clipboard, destPath);
                    }
                    else if (System.IO.File.Exists(_clipboard))
                    {
                        System.IO.File.Copy(_clipboard, destPath, true);
                    }
                    Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"粘贴失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileItem item)
            {
                var dialog = new InputDialog("重命名", "请输入新名称:") { };
        dialog.SetDefault(item.Name);
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
                {
                    try
                    {
                        var newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(item.FullPath)!, dialog.InputText);
                        if (item.IsDirectory)
                            System.IO.Directory.Move(item.FullPath, newPath);
                        else
                            System.IO.File.Move(item.FullPath, newPath);
                        Refresh();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"重命名失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileItem item)
            {
                var info = "";
                if (item.IsDirectory)
                {
                    var dirInfo = new System.IO.DirectoryInfo(item.FullPath);
                    info = $"📁 文件夹\n\n位置: {item.FullPath}\n创建时间: {dirInfo.CreationTime:g}";
                }
                else
                {
                    var fileInfo = new System.IO.FileInfo(item.FullPath);
                    info = $"📄 文件\n\n位置: {item.FullPath}\n大小: {FormatSize(fileInfo.Length)}\n创建时间: {fileInfo.CreationTime:g}\n修改时间: {fileInfo.LastWriteTime:g}";
                }
                MessageBox.Show(info, "属性", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileItem item)
            {
                var result = MessageBox.Show(
                    $"确定要删除 \"{item.Name}\" 吗？\n此操作不可撤销！",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (item.IsDirectory)
                            System.IO.Directory.Delete(item.FullPath, true);
                        else
                            System.IO.File.Delete(item.FullPath);
                        Refresh();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void OpenFolderNewTab_Click(object sender, RoutedEventArgs e)
        {
            if (QuickAccessList.SelectedItem is System.Windows.Controls.ListBoxItem item && item.Tag != null)
            {
                var path = Environment.ExpandEnvironmentVariables(item.Tag.ToString() ?? "");
                if (System.IO.Directory.Exists(path))
                {
                    OpenInNewTabRequested?.Invoke(this, path);
                }
            }
        }

        private void OpenDriveNewTab_Click(object sender, RoutedEventArgs e)
        {
            if (DriveList.SelectedItem is System.Windows.Controls.ListBoxItem item && item.Tag != null)
            {
                var path = item.Tag.ToString();
                if (!string.IsNullOrEmpty(path))
                {
                    OpenInNewTabRequested?.Invoke(this, path);
                }
            }
        }

        private void OpenFile(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void CopyDirectory(string source, string dest)
        {
            if (!System.IO.Directory.Exists(dest))
            {
                System.IO.Directory.CreateDirectory(dest);
            }
            
            foreach (var file in System.IO.Directory.GetFiles(source))
            {
                var destFile = System.IO.Path.Combine(dest, System.IO.Path.GetFileName(file));
                System.IO.File.Copy(file, destFile, true);
            }
            
            foreach (var dir in System.IO.Directory.GetDirectories(source))
            {
                var destDir = System.IO.Path.Combine(dest, System.IO.Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }

        private string _clipboard = "";
        private bool _isCutOperation = false;
        
        public event EventHandler<string>? OpenInNewTabRequested;
        public event EventHandler? RequestClose;

        private void ShowThisPC()
        {
            _isThisPCView = true;
            _currentPath = "thispc";
            TabName = "此电脑";
            
            ThisPCPanel.Visibility = Visibility.Visible;
            FileList.Visibility = Visibility.Collapsed;
            
            LoadDriveCards();
            UpdateStatusForThisPC();
            PathChanged?.Invoke(this, "此电脑");
        }

        private void LoadDriveCards()
        {
            DriveGrid.Items.Clear();
            
            // 添加常用文件夹
            AddFolderCard("桌面", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "🏠");
            AddFolderCard("下载", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads", "📥");
            AddFolderCard("文档", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "📄");
            AddFolderCard("图片", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "🖼️");
            AddFolderCard("音乐", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "🎵");
            AddFolderCard("视频", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "🎬");
            
            // 添加驱动器
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady)
                    {
                        AddDriveCard(drive);
                    }
                }
                catch { }
            }
        }

        private void AddFolderCard(string name, string path, string icon)
        {
            if (!Directory.Exists(path)) return;
            
            var btn = new Button
            {
                Width = 140,
                Height = 100,
                Margin = new Thickness(8),
                Background = (Brush)Application.Current.Resources["BgSecondary"],
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Tag = path
            };
            
            btn.Click += DriveCard_Click;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["FgPrimary"]
            };
            Grid.SetRow(iconText, 0);
            
            var nameText = new TextBlock
            {
                Text = name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["FgPrimary"],
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(nameText, 1);
            
            var infoText = new TextBlock
            {
                Text = "文件夹",
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["FgSecondary"]
            };
            Grid.SetRow(infoText, 2);
            
            grid.Children.Add(iconText);
            grid.Children.Add(nameText);
            grid.Children.Add(infoText);
            btn.Content = grid;
            
            DriveGrid.Items.Add(btn);
        }

        private void AddDriveCard(DriveInfo drive)
        {
            var totalGB = drive.TotalSize / 1024 / 1024 / 1024;
            var freeGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
            var usedGB = totalGB - freeGB;
            var usedPercent = totalGB > 0 ? (double)usedGB / totalGB * 100 : 0;
            
            var icon = drive.DriveType == DriveType.Fixed ? "💾" : 
                       drive.DriveType == DriveType.Removable ? "📱" : 
                       drive.DriveType == DriveType.CDRom ? "💿" : "💾";
            
            var btn = new Button
            {
                Width = 140,
                Height = 120,
                Margin = new Thickness(8),
                Background = (Brush)Application.Current.Resources["BgSecondary"],
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Tag = drive.RootDirectory.FullName
            };
            
            btn.Click += DriveCard_Click;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["FgPrimary"]
            };
            Grid.SetRow(iconText, 0);
            
            var nameText = new TextBlock
            {
                Text = drive.Name.TrimEnd('\\'),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["FgPrimary"],
                Margin = new Thickness(0, 4, 0, 0)
            };
            Grid.SetRow(nameText, 1);
            
            var sizeText = new TextBlock
            {
                Text = $"{totalGB:0.#} GB",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["FgSecondary"]
            };
            Grid.SetRow(sizeText, 2);
            
            // 进度条
            var progressBorder = new Border
            {
                Height = 6,
                Margin = new Thickness(8, 4, 8, 0),
                Background = (Brush)Application.Current.Resources["BgHover"],
                CornerRadius = new CornerRadius(3)
            };
            var usedColor = usedPercent > 80 ? Brushes.Red : 
                           usedPercent > 60 ? Brushes.Orange : 
                           (Brush)Application.Current.Resources["StatusBar"];
            var progressFill = new Border
            {
                Height = 6,
                Width = 124 * usedPercent / 100,
                Background = usedColor,
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            progressBorder.Child = progressFill;
            Grid.SetRow(progressBorder, 3);
            
            grid.Children.Add(iconText);
            grid.Children.Add(nameText);
            grid.Children.Add(sizeText);
            grid.Children.Add(progressBorder);
            btn.Content = grid;
            
            DriveGrid.Items.Add(btn);
        }

        private void DriveCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path)
            {
                NavigateTo(path);
            }
        }

        private void UpdateStatusForThisPC()
        {
            var driveCount = DriveInfo.GetDrives().Count(d => d.IsReady);
            StatusText.Text = $"🖥️ 此电脑 | {driveCount} 个驱动器";
        }

        private void LoadDrives()
        {
            DriveList.Items.Clear();
            DriveList.Items.Add(new ListBoxItem { Content = "🖥️ 此电脑", Tag = "thispc" });
            
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady)
                    {
                        DriveList.Items.Add(new ListBoxItem
                        {
                            Content = $"💾 {drive.Name} ({drive.TotalSize / 1024 / 1024 / 1024} GB)",
                            Tag = drive.RootDirectory.FullName
                        });
                    }
                }
                catch { }
            }
        }

        public void NavigateTo(string path)
        {
            try
            {
                if (path == "thispc")
                {
                    ShowThisPC();
                    return;
                }
                
                if (!Directory.Exists(path)) return;

                _isThisPCView = false;
                _currentPath = path;
                
                // 智能标签名
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var downloads = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                
                if (path == desktop) TabName = "桌面";
                else if (path == downloads) TabName = "下载";
                else if (path == docs) TabName = "文档";
                else if (path == pictures) TabName = "图片";
                else if (path == music) TabName = "音乐";
                else if (path == videos) TabName = "视频";
                else
                {
                    TabName = Path.GetFileName(path);
                    if (string.IsNullOrEmpty(TabName)) TabName = path;
                }
                
                _history.Push(path);
                _forwardHistory.Clear();

                ThisPCPanel.Visibility = Visibility.Collapsed;
                FileList.Visibility = Visibility.Visible;

                LoadFiles(path);
                UpdateStatus();
                PathChanged?.Invoke(this, path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFiles(string path)
        {
            _files.Clear();

            try
            {
                var dirs = Directory.GetDirectories(path);
                var files = Directory.GetFiles(path);

                if (!_settings.ShowHiddenFiles)
                {
                    dirs = dirs.Where(d => !Path.GetFileName(d).StartsWith(".")).ToArray();
                    files = files.Where(f => !Path.GetFileName(f).StartsWith(".")).ToArray();
                }

                dirs = dirs.OrderBy(d => d).ToArray();

                files = _settings.SortBy switch
                {
                    "name_desc" => files.OrderByDescending(f => f).ToArray(),
                    "size_asc" => files.OrderBy(f => new FileInfo(f).Length).ToArray(),
                    "size_desc" => files.OrderByDescending(f => new FileInfo(f).Length).ToArray(),
                    "date_desc" => files.OrderByDescending(f => new FileInfo(f).LastWriteTime).ToArray(),
                    "date_asc" => files.OrderBy(f => new FileInfo(f).LastWriteTime).ToArray(),
                    _ => files.OrderBy(f => f).ToArray()
                };

                foreach (var dir in dirs)
                {
                    var info = new DirectoryInfo(dir);
                    _files.Add(new FileItem
                    {
                        Name = info.Name,
                        FullPath = info.FullName,
                        Icon = "📁",
                        Info = "文件夹",
                        IsDirectory = true
                    });
                }

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    _files.Add(new FileItem
                    {
                        Name = info.Name,
                        FullPath = info.FullName,
                        Icon = GetFileIcon(info.Extension),
                        Info = FormatSize(info.Length),
                        IsDirectory = false
                    });
                }

                FileList.ItemsSource = _files;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("无权访问此文件夹", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string GetFileIcon(string ext)
        {
            return ext.ToLower() switch
            {
                ".txt" => "📝",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
                ".mp3" or ".wav" or ".flac" => "🎵",
                ".mp4" or ".avi" or ".mkv" => "🎬",
                ".zip" or ".rar" or ".7z" => "📦",
                ".pdf" => "📕",
                ".doc" or ".docx" => "📘",
                ".xls" or ".xlsx" => "📗",
                ".ppt" or ".pptx" => "📙",
                ".exe" or ".msi" => "⚙️",
                ".cs" or ".cpp" or ".h" or ".py" => "💻",
                ".js" or ".ts" => "🔵",
                ".html" or ".css" => "🌐",
                _ => "📄"
            };
        }

        private string FormatSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private void UpdateStatus()
        {
            var dirs = _files.Count(f => f.IsDirectory);
            var files = _files.Count(f => !f.IsDirectory);
            StatusText.Text = $"📁 {dirs} 个文件夹 | 📄 {files} 个文件 | 位置: {_currentPath}";
        }

        public void GoBack()
        {
            if (_history.Count > 1)
            {
                _forwardHistory.Push(_history.Pop());
                NavigateTo(_history.Peek());
            }
        }

        public void GoForward()
        {
            if (_forwardHistory.Count > 0)
            {
                var path = _forwardHistory.Pop();
                _history.Push(path);
                NavigateTo(path);
            }
        }

        public void GoUp()
        {
            if (_isThisPCView) return;
            
            var parent = Directory.GetParent(_currentPath);
            if (parent != null)
            {
                NavigateTo(parent.FullName);
            }
            else
            {
                ShowThisPC();
            }
        }

        public void Refresh()
        {
            if (_isThisPCView)
                LoadDriveCards();
            else if (_isSearchResults)
                // 搜索结果不刷新
                return;
            else
                NavigateTo(_currentPath);
        }

        public void ShowSearchResults(string keyword, List<string> results)
        {
            _isSearchResults = true;
            _currentPath = "search";
            TabName = $"🔍 {keyword}";
            
            _files.Clear();
            
            foreach (var path in results)
            {
                var isDir = System.IO.Directory.Exists(path);
                _files.Add(new FileItem
                {
                    Name = System.IO.Path.GetFileName(path),
                    FullPath = path,
                    Icon = isDir ? "📁" : GetFileIcon(System.IO.Path.GetExtension(path)),
                    Info = isDir ? "文件夹" : GetFileInfo(path),
                    IsDirectory = isDir
                });
            }
            
            ThisPCPanel.Visibility = Visibility.Collapsed;
            FileList.Visibility = Visibility.Visible;
            FileList.ItemsSource = _files;
            
            UpdateSearchStatus(keyword, results.Count);
            PathChanged?.Invoke(this, "search: " + keyword);
        }

        private string GetFileInfo(string path)
        {
            try
            {
                var info = new System.IO.FileInfo(path);
                return FormatSize(info.Length);
            }
            catch
            {
                return "";
            }
        }

        private void UpdateSearchStatus(string keyword, int count)
        {
            StatusText.Text = $"🔍 搜索 \"{keyword}\" 找到 {count} 个结果";
        }

        private bool _isSearchResults = false;

        private void QuickAccessList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QuickAccessList.SelectedItem is ListBoxItem item && item.Tag != null)
            {
                var path = Environment.ExpandEnvironmentVariables(item.Tag.ToString() ?? "");
                if (Directory.Exists(path))
                {
                    NavigateTo(path);
                }
                QuickAccessList.SelectedItem = null;
            }
        }

        private void DriveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DriveList.SelectedItem is ListBoxItem item && item.Tag != null)
            {
                var path = item.Tag.ToString();
                if (path == "thispc")
                {
                    ShowThisPC();
                }
                else if (Directory.Exists(path))
                {
                    NavigateTo(path);
                }
                DriveList.SelectedItem = null;
            }
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileList.SelectedItem is FileItem item)
            {
                if (item.IsDirectory)
                {
                    NavigateTo(item.FullPath);
                }
                else
                {
                    OpenFile(item.FullPath);
                }
            }
        }

        // ===== 简单输入对话框 =====
        public class InputDialog : System.Windows.Window
        {
            private System.Windows.Controls.TextBox _textBox;
            public string InputText => _textBox.Text;

            public InputDialog(string title, string prompt)
            {
                Title = title;
                Width = 350;
                Height = 150;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.NoResize;
                Background = (Brush)Application.Current.Resources["BgPrimary"];

                var grid = new Grid { Margin = new Thickness(16) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new TextBlock { Text = prompt, Foreground = (Brush)Application.Current.Resources["FgPrimary"], Margin = new Thickness(0, 0, 0, 8) };
                Grid.SetRow(label, 0);

                _textBox = new TextBox { Margin = new Thickness(0, 0, 0, 16), Padding = new Thickness(8, 4, 8, 4) };
                Grid.SetRow(_textBox, 1);

                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                var okButton = new Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
                okButton.Click += (s, ev) => { DialogResult = true; Close(); };
                var cancelButton = new Button { Content = "取消", Width = 80 };
                cancelButton.Click += (s, ev) => { DialogResult = false; Close(); };
                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 2);

                grid.Children.Add(label);
                grid.Children.Add(_textBox);
                grid.Children.Add(buttonPanel);
                Content = grid;
            }

            public void SetDefault(string text)
            {
                _textBox.Text = text;
            }
        }
    }
}