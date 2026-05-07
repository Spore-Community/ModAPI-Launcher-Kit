using Microsoft.Win32;
using System.IO;

namespace ModAPI.Common
{
    public static class SporePath
    {

        public enum Game
        {
            None,
            GalacticAdventures,
            Spore,
            CreepyAndCute
        }

        // Some things for Steam
        public static readonly string SteamAppsKey = @"HKEY_CURRENT_USER\Software\Valve\Steam\Apps\";
        public static readonly string GalacticAdventuresSteamID = "24720";

        private static string _sporebinEP1Path = null;

        /// <summary>
        /// Gets the path to the SporebinEP1 folder that contains SporeApp.exe for Galactic Adventures.
        /// The path will NOT have a trailing slash.
        /// If the path cannot be found or does not exist (typically if GA is not properly installed), this will return null.
        /// </summary>
        public static string GetSporebinEP1Path()
        {
            // Cache the result to avoid repeated filesystem lookups
            if (_sporebinEP1Path != null)
            {
                return _sporebinEP1Path;
            }

            // Use GA's data path as a starting point, as SporebinEP1 is always next to whatever GA's data dir is
            var dataPath = GetDataPath(Game.GalacticAdventures);
            if (dataPath == null)
            {
                return null;
            }

            var gameRootPath = Directory.GetParent(dataPath).FullName;
            var binPath = Path.Combine(gameRootPath, "SporebinEP1");

            // Verify that this folder actually contains SporeApp.exe
            if (!File.Exists(Path.Combine(binPath, "SporeApp.exe")))
            {
                return null;
            }

            return _sporebinEP1Path = binPath;
        }

        /// <summary>
        /// Gets the path to the Data folder that contains packages for the specified game.
        /// The path will NOT have a trailing slash.
        /// If the path cannot be found or does not exist (typically if the game is not properly installed), this will return null.
        /// </summary>
        public static string GetDataPath(Game game)
        {
            // If registry value is missing or not a string, return null
            if (!(GetFromRegistry(game, "DataDir") is string path))
            {
                return null;
            }

            // Remove "" if necessary
            path = FixPath(path);

            // Fix slashes if necessary (GOG .22 has mixed slashes)
            path = Path.GetFullPath(path);

            // Remove trailing slash if present
            path = path.TrimEnd(Path.DirectorySeparatorChar);

            // Verify that folder actually exists
            if (!Directory.Exists(path))
            {
                return null;
            }

            return path;
        }

        /// <summary>
        /// Returns true if the specified game is properly installed, by checking that its Data folder can be found and exists.
        /// This matches the behavior of the game itself, which shows a "Data directory missing or corrupt" error and exits if the Data folder (located via hardcoded registry key) cannot be found or does not exist.
        /// </summary>
        public static bool IsInstalled(Game game)
        {
            return GetDataPath(game) != null;
        }

        // remove "" if necessary
        private static string FixPath(string path)
        {
            if (path.StartsWith("\""))
            {
                return path.Substring(1, path.Length - 2);
            }
            else
            {
                return path;
            }
        }

        public static bool SporeIsInstalledOnSteam()
        {
            object result = Registry.GetValue(SteamAppsKey + GalacticAdventuresSteamID, "Installed", 0);
            // returns null if the key does not exist, or default value if the key existed but the value did not
            return result != null && ((int)result != 0);
        }

        /// <summary>
        /// Returns true if the game is installed such that all data folders share the same parent folder.
        /// Usually this doesn't matter, but as of early 2026, Steam may install multiple copies of the game due to differing app IDs. This can result in mods being installed to the wrong copy, so this function checks if this is the case.
        /// </summary>
        private static bool IsDataDirsSameParent()
        {
            // Get the data paths
            var sporeDataPath = GetDataPath(Game.Spore);
            var ccDataPath = GetDataPath(Game.CreepyAndCute); // null if not installed
            var gaDataPath = GetDataPath(Game.GalacticAdventures);

            // Get parent folder of each data path
            var sporeParent = sporeDataPath != null ? Directory.GetParent(sporeDataPath).FullName : null;
            var ccParent = ccDataPath != null ? Directory.GetParent(ccDataPath).FullName : null;
            var gaParent = gaDataPath != null ? Directory.GetParent(gaDataPath).FullName : null;

            // Make sure Spore and GA share the same parent folder
            if (sporeParent != gaParent)
            {
                return false;
            }

            // If CC is installed, it should also share the same parent
            if (ccParent != null && sporeParent != ccParent)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if Spore and GA are properly installed.
        /// If either game is not properly installed, returns false, and optionally shows an error message to the user. If the game is installed from Steam but hasn't been launched to complete the installation, shows a specific error message about launching the game on Steam.
        /// </summary>
        /// <returns></returns>
        public static bool IsGameInstalled(bool showMessageWhenFalse)
        {
            // Make sure data dirs are present for Spore and GA
            if (!IsInstalled(Game.Spore) || !IsInstalled(Game.GalacticAdventures))
            {
                if (showMessageWhenFalse)
                {
                    // Steam doesn't run the install script to create the registry keys until the game is launched from Steam for the first time
                    if (SporeIsInstalledOnSteam())
                    {
                        SupportInfo.ShowInfo(CommonStrings.SteamDownloadedButNotLaunched, CommonStrings.SteamDownloadedButNotLaunchedTitle, true, false);
                    }
                    else
                    {
                        SupportInfo.ShowError(CommonStrings.GameNotFound, CommonStrings.GameNotFoundTitle, false, false);
                    }
                }
                return false;
            }

            // For Steam only, check that all data dirs are in the same parent folder
            // As of early 2026, Steam may install multiple copies of the game due to differing app IDs. This can result in mods being installed to the wrong copy.
            // The "current" copy (Spore vs C&C vs GA) used is the most recent one launched from Steam. If we detect mixed folders, it means that non-GA has been launched, and the user should be told to launch GA to force everything to use GA's copy of the data.
            // Prior to early 2026, Steam only installed one copy, and they were all in the same folder, so this check will still pass.
            // NOTE: This may need to be changed in the future, if Steam changes anything again.
            if (SporeIsInstalledOnSteam() && !IsDataDirsSameParent())
            {
                if (showMessageWhenFalse)
                {
                    SupportInfo.ShowWarning(CommonStrings.SteamDownloadedButNotLaunched, CommonStrings.SteamDownloadedButNotLaunchedTitle);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the value of a registry key for the specified game, checking both 32-bit and 64-bit paths.
        /// If the key does not exist, returns null.
        /// </summary>
        private static object GetFromRegistry(Game game, string valueName)
        {
            foreach (string key in GetRegistryKeySuffixes(game))
            {
                var value = GetFromRegistry(key, valueName);
                if (value != null)
                {
                    return value;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the value of a registry key from HKLM\Software, checking both 32-bit and 64-bit paths.
        /// If the key does not exist, returns null.
        /// </summary>
        private static object GetFromRegistry(string keyName, string valueName)
        {
            // Registry key prefix varies depending on 64-bit or 32-bit system
            const string keyPrefix64 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\";
            const string keyPrefix32 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\";

            var value = Registry.GetValue(keyPrefix64 + keyName, valueName, null);
            return value ?? Registry.GetValue(keyPrefix32 + keyName, valueName, null);
        }

        /// <summary>
        /// Gets the possible registry subkeys within HKLM\Software for the specified game.
        /// These keys contain the DataDir value, which is used by the game to locate its own packages.
        /// </summary>
        private static string[] GetRegistryKeySuffixes(Game game)
        {
            switch (game)
            {
                case Game.GalacticAdventures:
                    return new[] { "Electronic Arts\\SPORE_EP1" };
                case Game.Spore:
                    return new[] { "Electronic Arts\\SPORE" };
                case Game.CreepyAndCute:
                    // Disc/Origin/EA use the former, Steam/GOG use the latter, game hardcodes both
                    return new[] { "Electronic Arts\\SPORE(TM) Creepy & Cute Parts Pack", "Electronic Arts\\SPORE Creepy and Cute Parts Pack" };
                default:
                    return new string[] { };
            }
        }

    }
}