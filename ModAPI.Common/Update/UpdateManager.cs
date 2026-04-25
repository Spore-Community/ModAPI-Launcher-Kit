using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ModAPI.Common.Dialog;

namespace ModAPI.Common.Update
{
    public static class UpdateManager
    {
        public static List<string> LauncherKitUpdateUrls = new List<string>
        {
            // Cloudflare R2 + Cache
            "https://update.launcherkit.sporecommunity.com/",
            // GitHub Releases
            "https://github.com/Spore-Community/modapi-launcher-kit/releases/latest/download/",
        };
        public static string PathPrefix = LauncherKitUpdateUrls.First();
        public static string AppDataPath = Environment.ExpandEnvironmentVariables(@"%appdata%\Spore ModAPI Launcher");
        public static string LastUpdateCheckTimePath = Path.Combine(AppDataPath, "lastUpdateCheckTime.info");
        public static string LastUpdateDateTimeFormat = "yyyy-MM-dd HH:mm";
        public static string UpdateInfoDestPath = Path.Combine(AppDataPath, "update.info");
        public static string UpdaterDestPath = Path.Combine(AppDataPath, "updater.exe");
        public static string UpdaterBlockPath = Path.Combine(AppDataPath, "noUpdateCheck.info");
        public static string UpdaterOverridePath = Path.Combine(AppDataPath, "overrideUpdatePath.info");
        public static Version CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        public static Version CurrentDllsBuild
        {
            get
            {
                string path = Path.Combine(Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString(), "coreLibs");
                List<Version> versions = new List<Version>();
                if (Directory.Exists(path))
                {
                    foreach (string s in Directory.EnumerateFiles(path).Where(x => x.EndsWith(".dll")))
                    {
                        string ver = FileVersionInfo.GetVersionInfo(s).FileVersion;
                        if (Version.TryParse(ver, out Version sVersion))
                            versions.Add(sVersion);
                    }
                }
                if (versions.Count() > 0)
                {
                    Version minVer = versions.Min();
                    return new Version(minVer.Major, minVer.Minor, minVer.Build, 0);
                }
                else
                    return new Version(999, 999, 999, 999);
            }
        }

        public static void CheckForUpdates()
        {
            if ((Process.GetProcessesByName("SporeApp").Length > 0) || (Process.GetProcessesByName("SporeApp_ModAPIFix").Length > 0))
            {
                SupportInfo.ShowWarning(CommonStrings.GameAlreadyRunning, CommonStrings.GameAlreadyRunningTitle, true, false);
                Process.GetCurrentProcess().Kill();
            }

            if (!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);

            File.WriteAllText(Path.Combine(AppDataPath, "path.info"), Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString());

            if (File.Exists(UpdaterDestPath))
                File.Delete(UpdaterDestPath);

            if (File.Exists(LastUpdateCheckTimePath))
            {
                try
                {
                    string lastUpdateCheckDateTimeString = File.ReadAllText(LastUpdateCheckTimePath);
                    DateTime lastUpdateCheckDateTime = DateTime.ParseExact(lastUpdateCheckDateTimeString,
                                                                        LastUpdateDateTimeFormat,
                                                                        CultureInfo.InvariantCulture);

                    if ((DateTime.Now - lastUpdateCheckDateTime).TotalHours < 1)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    File.Delete(LastUpdateCheckTimePath);
                }
            }

            if (File.Exists(UpdaterBlockPath))
            {
                // don't check for updates when block file exists
                return;
            }

            try
            {
                List<Exception> exceptions = new List<Exception>();
                bool didDownload = false;

                // Try to download the update info file from the override path first
                if (File.Exists(UpdaterOverridePath))
                {
                    PathPrefix = File.ReadAllText(UpdaterOverridePath);

                    // remove override if the URL is in our URL list
                    foreach (string url in LauncherKitUpdateUrls)
                    {
                        if (url == PathPrefix)
                        {
                            File.Delete(UpdaterOverridePath);
                            break;
                        }
                    }

                    try
                    {
                        using (var downloadClient = new DownloadClient(Path.Combine(PathPrefix, "update.info")))
                        {
                            downloadClient.SetTimeout(TimeSpan.FromSeconds(15));
                            downloadClient.DownloadToFile(UpdateInfoDestPath);
                        }

                        // Hides exceptions if the download was successful
                        didDownload = true;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
                // Try to download the update info file from each URL in the list
                else
                {
                    foreach (string url in LauncherKitUpdateUrls)
                    {
                        try
                        {
                            using (var downloadClient = new DownloadClient(Path.Combine(url, "update.info")))
                            {
                                downloadClient.SetTimeout(TimeSpan.FromSeconds(15));
                                downloadClient.DownloadToFile(UpdateInfoDestPath);
                            }

                            // Hides exceptions if the download was successful
                            didDownload = true;
                            PathPrefix = url;
                            break;
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }

                // If no download was successful, show all exceptions, one at a time
                if (!didDownload)
                {
                    ShowUpdateCheckFailedMessage(exceptions);

                    // early return when failed
                    return;
                }

                if (File.Exists(UpdateInfoDestPath))
                {
                    var updateInfoLines = File.ReadAllLines(UpdateInfoDestPath);
                    if (Version.TryParse(updateInfoLines[0], out Version ModApiSetupVersion) &&
                        ModApiSetupVersion == new Version(1, 0, 0, 0))
                    {
                        if (Version.Parse(updateInfoLines[1]) > CurrentVersion)
                        {
                            string currentLKVersion = SupportInfo.LauncherKitVersionString;
                            string newLKVersion = updateInfoLines[1];

                            if (MessageBox.Show(CommonStrings.LKUpdateAvailable.Replace("$NEWLK", newLKVersion).Replace("$CURRENTLK", currentLKVersion), CommonStrings.LKUpdateAvailableTitle, MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                if (bool.Parse(updateInfoLines[2]))
                                {
                                    Process.Start(updateInfoLines[3]);
                                }
                                else
                                {
                                    var dialog = new ProgressDialog(
                                        "Spore ModAPI Launcher Kit is updating to " + newLKVersion,
                                        "Spore ModAPI Launcher Kit updating",
                                        (s, e) =>
                                        {
                                            try
                                            {
                                                using (var downloadClient = new DownloadClient(updateInfoLines[3]))
                                                {
                                                    downloadClient.DownloadProgressChanged += (_, progress) =>
                                                    {
                                                        (s as BackgroundWorker).ReportProgress((int)(progress * 0.9f));
                                                    };

                                                    downloadClient.SetTimeout(TimeSpan.FromMinutes(5));
                                                    downloadClient.DownloadToFile(UpdaterDestPath);
                                                }

                                                if (File.Exists(UpdaterDestPath))
                                                {
                                                    var args = Environment.GetCommandLineArgs().ToList();

                                                    string currentArgs = string.Empty;
                                                    foreach (string arg in args)
                                                        currentArgs += "\"" + arg.TrimEnd('\\') + "\" ";

                                                    string argOnePath = Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString().TrimEnd('\\');
                                                    if (!argOnePath.EndsWith(" "))
                                                        argOnePath = argOnePath + " ";

                                                    Process.Start(UpdaterDestPath, "\"" + argOnePath + "\" " + currentArgs);
                                                    Process.GetCurrentProcess().Kill();
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                ShowUpdateCheckFailedMessage(new List<Exception>() { ex });
                                            }
                                        });
                                    dialog.ShowDialog();
                                }
                            }
                        }
                    }
                    else
                        ShowUnrecognizedUpdateInfoVersionMessage();
                }

                if (DllsUpdater.HasDllsUpdate(out var githubRelease))
                {
                    string newDllsVersion = githubRelease.tag_name;
                    string currentDllsVersion = SupportInfo.ModAPIDllsVersionString;
                    string currentLKVersion = SupportInfo.LauncherKitVersionString;
                    var result = MessageBox.Show(CommonStrings.DllsUpdateAvailable.Replace("$NEWDLLS", newDllsVersion).Replace("$CURRENTDLLS", currentDllsVersion).Replace("$CURRENTLK$", currentLKVersion), CommonStrings.DllsUpdateAvailableTitle, MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        var dialog = new ProgressDialog(
                            CommonStrings.UpdatingDllsDialog + githubRelease.tag_name,
                            CommonStrings.UpdatingDllsDialogTitle,
                            (s, e) =>
                            {
                                DllsUpdater.UpdateDlls(githubRelease, progress =>
                                {
                                    (s as BackgroundWorker).ReportProgress(progress);
                                });
                            });
                        dialog.ShowDialog();
                    }
                }

                File.WriteAllText(LastUpdateCheckTimePath, DateTime.Now.ToString(LastUpdateDateTimeFormat));
            }
            catch (Exception ex)
            {
                ShowUpdateCheckFailedMessage(new List<Exception>() { ex });
            }
        }

        static void ShowUpdateCheckFailedMessage(List<Exception> exceptions)
        {
            // show simplified exceptions
            // to prevent a big error dialog
            string exceptionText = "";
            foreach (var ex in exceptions)
            {
                exceptionText += ex.GetType().ToString() + ": " + ex.Message + "\n";
                if (ex.InnerException != null)
                {
                    exceptionText += ex.InnerException.GetType().ToString() + ": " + ex.InnerException.Message + "\n";
                    if (ex.InnerException.InnerException != null)
                    {
                        exceptionText += ex.InnerException.InnerException.GetType().ToString() + ": " + ex.InnerException.InnerException.Message + "\n";
                    }
                }
                exceptionText += "\n";

            }

            SupportInfo.ShowWarning(CommonStrings.UpdateCheckFailed + "\n\n" + exceptionText, CommonStrings.UpdateCheckFailedTitle, false, true);
        }

        static void ShowUnrecognizedUpdateInfoVersionMessage()
        {
            SupportInfo.ShowInfo("This update to the Spore ModAPI Launcher Kit must be downloaded manually. Please visit https://launcherkit.sporecommunity.com/support for more information.", CommonStrings.UpdateCheckFailedTitle, false, true);
        }
    }
}