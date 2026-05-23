using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using IWshRuntimeLibrary;

using Panel = System.Windows.Controls.Panel;
using File = System.IO.File;

namespace ModAPI.InterimSetup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool _isUpgradeInstall = false;
        int _currentPage = 0;
        bool _createDesktopShortcuts = true;
        string _currentLauncherKitPath = String.Empty;
        string[] _launcherkitExecutables = new string[]
        {
            "Spore ModAPI Easy Installer.exe",
            "Spore ModAPI Easy Uninstaller.exe",
            "Spore ModAPI Launcher.exe"
        };
        CircleEase _ease = new CircleEase()
        {
            EasingMode = EasingMode.EaseOut
        };
        TimeSpan _time = TimeSpan.FromMilliseconds(250);

        string _pathAcceptableText = string.Empty;
        string _pathNotAcceptableText = string.Empty;

        string _pageOneInstructionsUpgradeInstallText = "Please specify the path to your existing Spore ModAPI Launcher.";
        string _pageOneInstructionsNewInstallText = "Please specify where you would like to install the Spore ModAPI Launcher Kit.";

        string _pathSuffix = "Spore ModAPI Launcher Kit";
        string[] _forbiddenPrefixes = new string[]
        {
            Environment.ExpandEnvironmentVariables(@"%userprofile%\Desktop")
        };

        public MainWindow()
        {
            InitializeComponent();
            foreach (Panel p in RootGrid.Children)
            {
                if (RootGrid.Children.IndexOf(p) != _currentPage)
                    p.Visibility = Visibility.Collapsed;
            }

            FindExistingLauncherKit();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            CyclePage(_currentPage - 1);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            CyclePage(_currentPage + 1);
            BeginInstallation();
        }

        private void FindExistingLauncherKit()
        {
            try
            {
                string pathInfo = Environment.ExpandEnvironmentVariables(@"%appdata%\Spore ModAPI Launcher\path.info");
                string launcherKitPath;
                if (File.Exists(pathInfo))
                {
                    launcherKitPath = File.ReadAllText(pathInfo);
                    if (VerifyPath(launcherKitPath, true))
                    {
                        _currentLauncherKitPath = launcherKitPath;

                        // when found, redirect directly to the upgrade page
                        UpgradeInstallButton_Click(this, null);
                    }
                }
            }
            catch (Exception)
            {
                // do nothing
            }
        }

        private void UnzipLauncherKit(string path)
        {
            using (MemoryStream unmStream = new MemoryStream(ModAPI.InterimSetup.Properties.Resources.ModApiUpdate))
            {
                using (ZipArchive archive = new ZipArchive(unmStream, ZipArchiveMode.Read))
                {
                    int archiveEntryCount = archive.Entries.Count;
                    Dispatcher.BeginInvoke(new Action(() => InstallProgressBar.Maximum = archiveEntryCount));

                    foreach (var s in archive.Entries)
                    {
                        string fileOutPath = Path.Combine(path, s.FullName);

                        if (File.Exists(fileOutPath))
                            File.Delete(fileOutPath);

                        string extractDir = Directory.GetParent(fileOutPath).ToString();
                        if (!Directory.Exists(extractDir))
                            Directory.CreateDirectory(extractDir);

                        if (!s.FullName.EndsWith("/") &&
                            !s.FullName.EndsWith("\\"))
                        {
                            s.ExtractToFile(fileOutPath);
                        }

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (InstallProgressBar.Value < InstallProgressBar.Maximum)
                                InstallProgressBar.Value++;
                        }));
                    }
                    string noUpdateFilePath = Environment.ExpandEnvironmentVariables(@"%appdata%\Spore ModAPI Launcher\noUpdateCheck.info");
                    if (File.Exists(noUpdateFilePath))
                        File.Delete(noUpdateFilePath);
                }
            }
        }

        private void BeginInstallation()
        {
            string path = PathTextBox.Text;
            if (File.Exists(path))
                path = Directory.GetParent(path).ToString();

            if (!_isUpgradeInstall)
            {
                if (!path.ToLowerInvariant().EndsWith(_pathSuffix.ToLowerInvariant()))
                    path = Path.Combine(path, _pathSuffix);
            }

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                UnzipLauncherKit(path);

                string launcherShortcutPath = "Spore ModAPI Launcher.lnk";
                string launcherDesc = "Open Spore through the Spore ModAPI Launcher";
                string easyInstallerShortcutPath = "Spore ModAPI Easy Installer.lnk";
                string easyInstallerDesc = "Quickly and easily install mods for Spore";
                string easyUninstallerShortcutPath = "Spore ModAPI Easy Uninstaller.lnk";
                string easyUninstallerDesc = "Quickly and easily configure and remove mods for Spore";

                string startPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Spore ModAPI Launcher Kit");
                if (!Directory.Exists(startPath))
                    Directory.CreateDirectory(startPath);

                CreateShortcut(launcherShortcutPath, launcherDesc, Path.Combine(path, "Spore ModAPI Launcher.exe"), true);
                CreateShortcut(easyInstallerShortcutPath, easyInstallerDesc, Path.Combine(path, "Spore ModAPI Easy Installer.exe"), true);
                CreateShortcut(easyUninstallerShortcutPath, easyUninstallerDesc, Path.Combine(path, "Spore ModAPI Easy Uninstaller.exe"), true);

                if (_createDesktopShortcuts)
                {
                    CreateShortcut(launcherShortcutPath, launcherDesc, Path.Combine(path, "Spore ModAPI Launcher.exe"));
                    CreateShortcut(easyInstallerShortcutPath, easyInstallerDesc, Path.Combine(path, "Spore ModAPI Easy Installer.exe"));
                    CreateShortcut(easyUninstallerShortcutPath, easyUninstallerDesc, Path.Combine(path, "Spore ModAPI Easy Uninstaller.exe"));

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        CompletionInfoTextBlock.Text = "The Spore ModAPI Launcher kit has been successfully installed. Shortcuts to access it have been created on your Desktop. You can access it from those. Note that you must launch Spore through the Spore ModAPI Launcher from now on.";
                    }));
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        CompletionInfoTextBlock.Text = "The Spore ModAPI Launcher kit has been successfully installed. Note that you must launch Spore through the Spore ModAPI Launcher from now on.";
                    }));
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CyclePage(3);
                }));
            }).Start();
        }

        private void UpgradeInstallButton_Click(object sender, RoutedEventArgs e)
        {
            _isUpgradeInstall = true;
            _pathAcceptableText = "This path is valid. An existing ModAPI Launcher was found.";
            _pathNotAcceptableText = "This path is not valid. No existing ModAPI Launcher was found to upgrade.";
            PageOneInstructionsTextBlock.Text = _pageOneInstructionsUpgradeInstallText;
            PathTextBox.Text = _currentLauncherKitPath;
            CyclePage(1);
        }

        private void NewInstallButton_Click(object sender, RoutedEventArgs e)
        {
            _isUpgradeInstall = false;
            _pathAcceptableText = "This path is valid. The ModAPI Launcher Kit can be installed here.";
            _pathNotAcceptableText = "This path is not valid. The ModAPI Launcher Kit cannot be installed here.";
            PageOneInstructionsTextBlock.Text = _pageOneInstructionsNewInstallText;
            PathTextBox.Text = Environment.ExpandEnvironmentVariables(@"%programdata%\SPORE ModAPI Launcher Kit");
            CyclePage(1);
        }

        public void CyclePage(int targetPage)
        {
            RootGrid.Children[targetPage].Visibility = Visibility.Visible;

            DoubleAnimation GlideInAnimation = new DoubleAnimation()
            {
                Duration = _time,
                EasingFunction = _ease,
                From = 50,
                To = 0
            };
            DoubleAnimation GlideOutAnimation = new DoubleAnimation()
            {
                Duration = _time,
                EasingFunction = _ease,
                From = 0,
                To = -50
            };
            DoubleAnimation FadeInAnimation = new DoubleAnimation()
            {
                Duration = _time,
                EasingFunction = _ease,
                From = 0,
                To = 1
            };
            DoubleAnimation FadeOutAnimation = new DoubleAnimation()
            {
                Duration = _time,
                EasingFunction = _ease,
                From = 1,
                To = 0
            };
            FadeOutAnimation.Completed += (sneder, args) =>
            {
                RootGrid.Children[_currentPage].Visibility = Visibility.Collapsed;
                _currentPage = targetPage;
            };

            RootGrid.Children[_currentPage].RenderTransform = new TranslateTransform();
            RootGrid.Children[targetPage].RenderTransform = new TranslateTransform();

            RootGrid.Children[_currentPage].BeginAnimation(Panel.OpacityProperty, FadeOutAnimation);
            (RootGrid.Children[_currentPage].RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.XProperty, GlideOutAnimation);
            RootGrid.Children[targetPage].BeginAnimation(Panel.OpacityProperty, FadeInAnimation);
            (RootGrid.Children[targetPage].RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.XProperty, GlideInAnimation);

            if (targetPage == 1)
                FooterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    To = 1
                });
            else
                FooterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    To = 0
                });

            if (targetPage == 3)
                SecondFooterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    To = 1
                });
            else
                SecondFooterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    To = 0
                });
        }

        private void PathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpgradeInstall)
            {
                OpenFileDialog dialog = new OpenFileDialog()
                {
                    Title = "Select the path to your existing Spore ModAPI Launcher.",
                    Filter = "Spore ModAPI Executable (*.exe)|" + String.Join(";", _launcherkitExecutables),
                    FilterIndex = 0,
                    CheckPathExists = true
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    PathTextBox.Text = dialog.FileName;
                }
            }
            else
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog()
                {
                    Description = "Select where you would like to install the Spore ModAPI Launcher Kit.",
                };

                string selectedPath = Environment.ExpandEnvironmentVariables(PathTextBox.Text);
                if (IsValidPath(selectedPath))
                    dialog.SelectedPath = selectedPath;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    PathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        bool VerifyPath(string path, bool forceUpgradeCheck = false)
        {
            if (forceUpgradeCheck || _isUpgradeInstall)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        path = Path.GetFileName(path).ToLower();
                        foreach (var executable in _launcherkitExecutables)
                        {
                            if (path == executable.ToLower())
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        foreach (var executable in _launcherkitExecutables)
                        {
                            if (File.Exists(Path.Combine(path, executable)))
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // do nothing
                }

                return false;
            }
            else
            {
                if (!IsValidPath(PathTextBox.Text))
                {
                    return false;
                }

                // ensure users don't install to the Desktop or Program Files
                string lowerPathText = PathTextBox.Text.ToLowerInvariant();
                string desktopPath = Environment.ExpandEnvironmentVariables(@"%userprofile%\Desktop").ToLowerInvariant();
                if (lowerPathText.StartsWith(desktopPath))
                {
                    return false;
                }

                string[] programFilesEnvironmentVariables =
                {
                    "ProgramFiles",
                    "ProgramFiles(X86)",
                    "ProgramW6432",
                };

                foreach (var environmentVariable in programFilesEnvironmentVariables)
                {
                    string envPath = Environment.GetEnvironmentVariable(environmentVariable).ToLowerInvariant();

                    if (!String.IsNullOrEmpty(envPath) && lowerPathText.StartsWith(envPath))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        //https://stackoverflow.com/questions/3137097/check-if-a-string-is-a-valid-windows-directory-folder-path
        private bool IsValidPath(string path, bool exactPath = true)
        {
            bool isValid = true;

            try
            {
                string fullPath = Path.GetFullPath(path);

                if (exactPath)
                {
                    string root = Path.GetPathRoot(path);
                    isValid = string.IsNullOrEmpty(root.Trim(new char[] { '\\', '/' })) == false;
                }
                else
                {
                    isValid = Path.IsPathRooted(path);
                }
            }
            catch (Exception)
            {
                isValid = false;
            }

            foreach (string s in _forbiddenPrefixes)
            {
                if (path.ToLowerInvariant().StartsWith(s.ToLowerInvariant()))
                {
                    isValid = false;
                    break;
                }
            }

            return isValid;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //e.Cancel = true;
        }

        private void PathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (VerifyPath(PathTextBox.Text))
            {
                GoodOrBadPathTextBlock.Text = _pathAcceptableText;
                GoodOrBadPathTextBlock.Foreground = new SolidColorBrush(Colors.ForestGreen);
                NextButton.IsEnabled = true;
            }
            else
            {
                GoodOrBadPathTextBlock.Text = _pathNotAcceptableText;
                GoodOrBadPathTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                NextButton.IsEnabled = false;
            }

            if (!string.IsNullOrWhiteSpace(PathTextBox.Text)) // && (File.Exists(PathTextBox.Text) || Directory.Exists(PathTextBox.Text))
                GoodOrBadPathTextBlock.Visibility = Visibility.Visible;
            else
                GoodOrBadPathTextBlock.Visibility = Visibility.Hidden;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CreateShortcut(string shortcutFileName, string description, string executablePath, bool inStartMenu = false)
        {
            string shortcutPath;
            if (inStartMenu)
            {
                shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), shortcutFileName);
            }
            else
            { // fallback to desktop
                shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), shortcutFileName);
            }

            IWshShortcut shortcut = (IWshShortcut)new WshShell().CreateShortcut(shortcutPath);
            shortcut.Description = description;
            shortcut.TargetPath = executablePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(executablePath);
            shortcut.Save();
        }

        private void ShortcutsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _createDesktopShortcuts = true;
        }

        private void ShortcutsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _createDesktopShortcuts = false;
        }
    }
}