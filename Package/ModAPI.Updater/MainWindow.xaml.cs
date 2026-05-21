using ModAPI.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Input;

namespace ModAPI.Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] args = Environment.GetCommandLineArgs();
        bool launcherExists = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (args.Length < 2)
            {
                Hide();
                launcherExists = false;
                MessageBox.Show("Please launch the updater with the directory where Spore ModAPI Launcher Kit is installed as first commandline argument");
                Close();
                return;
            }

            // trim is required here because for some reason the launcher kit
            // adds an empty space at the end of the installed path
            string path = args[1].TrimEnd();

            bool foundLauncher = File.Exists(Path.Combine(path, "Spore ModAPI Launcher.exe"));
            if (foundLauncher)
            {
                using (MemoryStream unmStream = new MemoryStream(ModAPI.Updater.Properties.Resources.ModApiUpdate))
                {
                    using (ZipArchive archive = new ZipArchive(unmStream, ZipArchiveMode.Read))
                    {
                        InstallProgressBar.Maximum = archive.Entries.Count;

                        foreach (var s in archive.Entries)
                        {
                            string fileOutPath = Path.Combine(path, s.FullName);

                            if (File.Exists(fileOutPath))
                                File.Delete(fileOutPath);

                            if (!s.FullName.EndsWith("/") &&
                                !s.FullName.EndsWith("\\") &&
                                IsPathNotPartOfConfiguration(fileOutPath))
                            {
                                s.ExtractToFile(fileOutPath);
                            }

                            if (InstallProgressBar.Value < InstallProgressBar.Maximum)
                                InstallProgressBar.Value++;
                        }
                        string appDataPath = Environment.ExpandEnvironmentVariables(@"%appdata%\Spore ModAPI Launcher");
                        string noUpdateFilePath = Path.Combine(appDataPath, "noUpdateCheck.info");
                        if (File.Exists(noUpdateFilePath))
                            File.Delete(noUpdateFilePath);

                        StateTextBlock.Text = "Updates were installed successfully.";
                        InstallProgressBar.Visibility = Visibility.Collapsed;
                        AcknowledgeButton.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                Hide();
                MessageBox.Show("No Spore ModAPI Launcher Kit could be found in the following directory:\n" + path);
                Close();
            }
        }

        private bool IsPathNotPartOfConfiguration(string extractToPath)
        {
            string path = extractToPath.ToLowerInvariant();
            if (path.Contains(@"\modconfigs"))
                return false;
            else if (path.Contains(@"\mlibs"))
                return false;
            else if (path.EndsWith(@"\sporemodapi-disk.dll", StringComparison.OrdinalIgnoreCase))
                return true;
            else if (path.EndsWith(@"\sporemodapi-steam_patched.dll", StringComparison.OrdinalIgnoreCase))
                return true;
            else if (path.EndsWith("-disk.dll", StringComparison.OrdinalIgnoreCase))
                return false;
            else if (path.EndsWith("-steam.dll", StringComparison.OrdinalIgnoreCase))
                return false;
            else if (path.EndsWith("-steam_patched.dll", StringComparison.OrdinalIgnoreCase))
                return false;
            else if (path.EndsWith("installedmods.config", StringComparison.OrdinalIgnoreCase))
                return false;
            else if (path.EndsWith("launchersettings.config", StringComparison.OrdinalIgnoreCase))
                return false;
            else if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                return false;
            else
                return true;
        }

        private void AcknowledgeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PreCloseWindowActions();
        }

        private void PreCloseWindowActions()
        {
            if (launcherExists && args.Length > 2)
            {
                string pathArg = args[2];
                string exeArgs = string.Empty;

                if (args.Length > 3)
                {
                    for (int i = 3; i < args.Length; i++)
                    {
                        string currentArg = args[i];

                        if (!(currentArg.StartsWith("\"")))
                            currentArg = "\"" + currentArg;

                        if (!(currentArg.EndsWith("\"")))
                            currentArg = currentArg + "\"";

                        exeArgs += currentArg + " ";
                    }
                }
                
                if (Permissions.IsAdministrator() && Path.GetFileName(pathArg).ToLowerInvariant().Contains("modapi launcher"))
                {
                    MessageBox.Show("The Launcher Kit has been updated. Open the Spore ModAPI Launcher to launch the game.");
                }
                else
                {
                    Process.Start(pathArg, exeArgs);
                }
            }
        }
    }
}