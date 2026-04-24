using ModAPI.Common.Update;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ModAPI.Common
{
    public static class SupportInfo
    {
        /// <summary>
        /// The current version of the Launcher Kit.
        /// </summary>
        public static Version LauncherKitVersion => UpdateManager.CurrentVersion;

        /// <summary>
        /// The current version of the Launcher Kit, as a string containing a minimum of three version components (i.e. "1.2.3"). A fourth component (revision) will be included only if it is non-zero.
        /// </summary>
        public static string LauncherKitVersionString => LauncherKitVersion.Revision == 0 ? LauncherKitVersion.ToString(3) : LauncherKitVersion.ToString();

        /// <summary>
        /// The current version of the ModAPI DLLs that will be injected into the game.
        /// </summary>
        public static Version ModAPIDllsVersion => UpdateManager.CurrentDllsBuild;

        /// <summary>
        /// The current version of the ModAPI DLLs, as a string containing a minimum of three version components (i.e. "1.2.3"). A fourth component (revision) will be included only if it is non-zero.
        /// </summary>
        public static string ModAPIDllsVersionString => ModAPIDllsVersion.Revision == 0 ? ModAPIDllsVersion.ToString(3) : ModAPIDllsVersion.ToString();

        /// <summary>
        /// The folder path where the Launcher Kit is installed. This folder contains the executables for the Easy Installer, Easy Uninstaller, and Launcher.
        /// By default, this is "%programdata%\Spore ModAPI Launcher Kit".
        /// The path will NOT have a trailing slash.
        /// </summary>
        public static String LauncherKitPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);



        /// <summary>
        /// The file version of SporeApp.exe.
        /// If SporeApp.exe could not be found, this will return null.
        /// </summary>
        public static string GameVersionString
        {
            get
            {
                var sporebinEP1Path = SporePath.GetSporebinEP1Path();
                if (sporebinEP1Path == null)
                {
                    return null;
                }
                // If not null, SporebinEP1Path is guaranteed to exist and contain SporeApp.exe
                var sporeAppPath = Path.Combine(sporebinEP1Path, "SporeApp.exe");
                return FileVersionInfo.GetVersionInfo(sporeAppPath).FileVersion;
            }
        }

        /// <summary>
        /// The version type of the game. This will be one of the following values:
        /// "Disc + Patch 5.1, July 2009",
        /// "Origin, March 2017",
        /// "EA App, October 2024",
        /// "GOG/Steam, March 2017",
        /// "GOG, October 2024",
        /// "Steam, October 2024",
        /// "Unknown".
        /// If SporeApp.exe could not be found, this will return null.
        /// </summary>
        public static string GameVersionTypeString
        {
            get
            {
                var sporebinEP1Path = SporePath.GetSporebinEP1Path();
                if (sporebinEP1Path == null)
                {
                    return null;
                }
                var sporeAppPath = Path.Combine(sporebinEP1Path, "SporeApp.exe");
                var versionType = GameVersion.DetectVersion(sporeAppPath);
                return GameVersion.GetFriendlyVersionName(versionType);
            }
        }

        /// <summary>
        /// Gets a string with the game version and version type, i.e. "3.1.0.29 - GOG, October 2024, LAA".
        /// If SporeApp.exe could not be found, this will return "Game not found".
        /// </summary>
        public static string GameFullVersionInfoString
        {
            get
            {
                if (GameVersionString == null || GameVersionTypeString == null)
                {
                    return "Game not found";
                }

                var sporeAppPath = Path.Combine(SporePath.GetSporebinEP1Path(), "SporeApp.exe");
                var laaString = LAAUtils.IsLAA(sporeAppPath) ? ", LAA" : "";

                return $"{GameVersionString} - {GameVersionTypeString}{laaString}";
            }
        }



        private static DialogResult ShowMessageBox(string message, string title, bool showGameInfo, bool showPaths, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            message += "\n";

            // Append game info
            if (showGameInfo)
            {
                message += $"\nSpore version: {GameFullVersionInfoString}";
                if (showPaths)
                {
                    message += $"\nSpore path: {SporePath.GetSporebinEP1Path()}";
                }
            }

            // Append LK info
            message += $"\nLauncher Kit version: {LauncherKitVersionString}\nModAPI DLLs Version: {ModAPIDllsVersionString}";
            if (showPaths)
            {
                message += $"\nLauncher Kit path: {LauncherKitPath}";
            }

            return MessageBox.Show(message, title, buttons, icon);
        }

        /// <summary>
        /// Shows an info message box to the user. Information about the Launcher Kit will be included, and optionally information about the game.
        /// Info message boxes should be used for information that the user should be aware of, but is not causing a problem.
        /// </summary>
        public static DialogResult ShowInfo(string message, string title, bool showGameInfo = true, bool showPaths = true, MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            return ShowMessageBox(message, title, showGameInfo, showPaths, buttons, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Shows a warning message box to the user. Information about the Launcher Kit will be included, and optionally information about the game.
        /// Warning message boxes should be used for potential problems, where the program can continue, but further problems are likely to occur.
        /// </summary>
        public static DialogResult ShowWarning(string message, string title, bool showGameInfo = true, bool showPaths = true, MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            return ShowMessageBox(message, title, showGameInfo, showPaths, buttons, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Shows an error message box to the user. Information about the Launcher Kit will be included, and optionally information about the game.
        /// Error message boxes should be used for problems that prevent the program from continuing.
        /// </summary>
        public static DialogResult ShowError(string message, string title, bool showGameInfo = true, bool showPaths = true, MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            return ShowMessageBox(message, title, showGameInfo, showPaths, buttons, MessageBoxIcon.Error);
        }

    }
}