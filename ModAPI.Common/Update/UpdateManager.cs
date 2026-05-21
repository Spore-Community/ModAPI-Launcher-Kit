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
                    return new Version(minVer.Major, minVer.Minor, minVer.Build, 0); // revision must be 0 for update checking purposes, as we use revision to indicate whether the DLL is for disc/download/combined versions
                }
                else
                    return new Version(999, 999, 999, 999);
            }
        }
        public static bool IsUpdateCheckDisabled => File.Exists(UpdaterBlockPath);

        public static void CheckForUpdates()
        {
            // Wrap in try-catch in case an exception occurs while reading or writing any files/folders
            try
            {
                // Create appdata folder if it doesn't exist
                Directory.CreateDirectory(AppDataPath);

                // Try to write support info file to appdata folder
                SupportInfo.WriteSupportInfoFile(Path.Combine(AppDataPath, "support.info"));

                // Write Launcher Kit path to path.info
                File.WriteAllText(Path.Combine(AppDataPath, "path.info"), SupportInfo.LauncherKitPath);

                // Make sure game is not running before running any Launcher Kit apps
                if ((Process.GetProcessesByName("SporeApp").Length > 0) || (Process.GetProcessesByName("SporeApp_ModAPIFix").Length > 0))
                {
                    SupportInfo.ShowWarning(CommonStrings.GameAlreadyRunning, CommonStrings.GameAlreadyRunningTitle, true, false);
                    Process.GetCurrentProcess().Kill();
                }

                // Delete old updater exe if present
                if (File.Exists(UpdaterDestPath))
                {
                    File.Delete(UpdaterDestPath);
                }

                if (File.Exists(LastUpdateCheckTimePath))
                {
                    try
                    {
                        string lastUpdateCheckDateTimeString = File.ReadAllText(LastUpdateCheckTimePath);
                        DateTime lastUpdateCheckDateTime = DateTime.ParseExact(lastUpdateCheckDateTimeString,
                                                                            LastUpdateDateTimeFormat,
                                                                            CultureInfo.InvariantCulture);

                        // If it's been less than an hour since last update check, don't check again
                        if ((DateTime.Now - lastUpdateCheckDateTime).TotalHours < 1)
                        {
                            return;
                        }

                        // If update check is disabled, and it's been more than 30 days since last update check, prompt user to check for updates
                        if (IsUpdateCheckDisabled && (DateTime.Now - lastUpdateCheckDateTime).TotalDays >= 30)
                        {
                            PromptUserUnblockUpdates();
                        }
                    }
                    catch (FormatException)
                    {
                        ResetLastUpdateCheckTime();
                    }
                }
                else if (IsUpdateCheckDisabled)
                {
                    // If update check is disabled, and this is a new install or we force checked for updates, prompt user to check for updates
                    PromptUserUnblockUpdates();
                }

                // Record current time as last update check time
                File.WriteAllText(LastUpdateCheckTimePath, DateTime.Now.ToString(LastUpdateDateTimeFormat));
            }
            catch (Exception ex)
            {
                // Just in case there's a problem with SupportInfo, use basic MessageBox
                MessageBox.Show(CommonStrings.UpdatePreCheckFailed + "\n\n" + ex.ToString(), CommonStrings.UpdateCheckFailedTitle);
            }

            // Don't check for updates when block file exists
            if (IsUpdateCheckDisabled)
            {
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

                            if (MessageBox.Show(CommonStrings.LKUpdateAvailable.Replace("$NEWLK$", newLKVersion).Replace("$CURRENTLK$", currentLKVersion), CommonStrings.LKUpdateAvailableTitle, MessageBoxButtons.YesNo) == DialogResult.Yes)
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
                    var result = MessageBox.Show(CommonStrings.DllsUpdateAvailable.Replace("$NEWDLLS$", newDllsVersion).Replace("$CURRENTDLLS$", currentDllsVersion).Replace("$CURRENTLK$", currentLKVersion), CommonStrings.DllsUpdateAvailableTitle, MessageBoxButtons.YesNo);
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
            SupportInfo.ShowInfo(CommonStrings.UpdateUnrecognized, CommonStrings.UpdateCheckFailedTitle, false, true);
        }

        /// <summary>
        /// Forces the Launcher Kit to check for updates the next time it is run, skipping the one hour timeout.
        /// If updates are blocked, this will also prompt the user to unblock updates on next launch.
        /// </summary>
        public static void ResetLastUpdateCheckTime()
        {
            try
            {
                File.Delete(LastUpdateCheckTimePath);
            }
            catch { } // If the file can't be deleted, it probably can't be read, in which case we always do an update check anyway
        }

        /// <summary>
        /// Warns the user that update checks are disabled, and prompts them to re-enable.
        /// Returns Yes or No depending on whether the user wants to re-enable update checks. If Yes, the update block file is removed.
        /// </summary>
        static DialogResult PromptUserUnblockUpdates()
        {
            var result = SupportInfo.ShowInfo(CommonStrings.UpdateCheckDisabledNotice, CommonStrings.UpdateCheckDisabledNoticeTitle, false, false, MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                File.Delete(UpdaterBlockPath);
            }
            return result;
        }

    }
}