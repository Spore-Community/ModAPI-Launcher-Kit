using System;
using System.Collections.Generic;
using System.Windows.Forms;

using System.Diagnostics;
using System.Text;

using System.IO;

using ModAPI.Common;
using ModAPI.Common.Types;
using ModAPI.Common.Update;

namespace Spore_ModAPI_Easy_Uninstaller
{

    public static class EasyUninstaller
    {

        private static InstalledMods Mods;

        private static UninstallerForm Form;


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!Permissions.IsAdministrator())
            {
                UpdateManager.CheckForUpdates();
                Permissions.RerunAsAdministrator();
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // ensure we find Spore & GA as early as possible
                if (!SporePath.IsGameInstalled(true))
                {
                    return;
                }

                Form = new UninstallerForm();

                Mods = new InstalledMods();
                Mods.Load();
                ReloadMods();

                Application.Run(Form);
            }
        }

        public static void ReloadMods()
        {
            Mods.Load();
            Form.AddMods(Mods.ModConfigurations);
        }

        public static void UninstallMods(Dictionary<ModConfiguration, bool> mods)
        {
            List<ModConfiguration> successfulMods = new List<ModConfiguration>();

            try
            {
                foreach (var mod in mods)
                {
                    ResultType result = ResultType.Success;

                    if (mod.Value)
                    {
                        result = ExecuteConfigurator(mod.Key, true);
                    }
                    else
                    {
                        RemoveModFiles(mod.Key);
                    }

                    if (result == ResultType.Success)
                    {
                        Mods.RemoveMod(mod.Key);
                        successfulMods.Add(mod.Key);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(CommonStrings.UnauthorizedAccess, Strings.CouldNotUninstall, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.CouldNotUninstall, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (successfulMods.Count > 0)
            {
                Mods.Save();

                // reload before we show the message box
                EasyUninstaller.ReloadMods();

                var sb = new StringBuilder();
                foreach (ModConfiguration mod in successfulMods)
                {
                    sb.Append(mod);
                    sb.Append("\n");
                }

                MessageBox.Show(Strings.ModsWereUninstalled + "\n" + sb.ToString(), Strings.UninstallationSuccessful);
            }
        }

        private static string GetOutputPath(string pathType)
        {
            switch (pathType)
            {
                case "GalacticAdventures":
                    return SporePath.GetDataPath(SporePath.Game.GalacticAdventures);

                case "Spore":
                    return SporePath.GetDataPath(SporePath.Game.Spore);

                case "None":
                    return Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString();

                default:
                    return null;
            }
        }

        // returns number of files with errors
        private static void RemoveModFiles(ModConfiguration mod)
        {
            foreach (InstalledFile file in mod.InstalledFiles)
            {
                string outputPath = GetOutputPath(file.PathType);

                if (outputPath == null)
                {
                    throw new Exception(Strings.CouldNotUninstall + " \"" + mod.Name + "\"\n" + CommonStrings.GameNotFound);
                }

                string outputFile = Path.Combine(outputPath, file.Name);
                string outputFile2 = Path.Combine(outputPath, "mLibs", file.Name);
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
                if (File.Exists(outputFile2)) //if ((file.Name.Contains("-disk") || file.Name.Contains("-steam") || file.Name.Contains("-steam_patched")))
                    File.Delete(outputFile2);
            }

            string modConfigPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "ModConfigs", mod.Name);
            if (Directory.Exists(modConfigPath))
                Directory.Delete(modConfigPath, true);
        }

        private static string ConvertToArgument(string path)
        {
            if (path == null)
            {
                return "null";
            }
            else
            {
                return "\"" + path + "\"";
            }
        }

        public static ResultType ExecuteConfigurator(ModConfiguration mod, bool uninstall)
        {
            string path = Path.Combine(
                    Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString()
                    , "Spore ModAPI Easy Installer.exe");
            string args = "\"" + mod.Name + "\"" + " true " + uninstall.ToString();
            var process = Process.Start(path, args);
            process.WaitForExit();
            return (ResultType)process.ExitCode;
        }
    }
}
