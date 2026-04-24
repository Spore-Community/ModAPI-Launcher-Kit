using EnvDTE80;
using ModAPI.Common;
using ModAPI.Common.Dialog;
using ModAPI.Common.Update;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SporeModAPI_Launcher
{

    class Program
    {
        private const string ModAPIFixDownloadURL = "https://davoonline.com/sporemodder/emd4600/SporeApp_ModAPIFix.zip";
        private const string ModApiHelpThreadURL = "https://launcherkit.sporecommunity.com/support";

        private string SporebinPath;
        private string ExecutablePath;
        private GameVersionType ExecutableType;

        // Used for executing Spore and injecting DLLs
        private NativeTypes.STARTUPINFO StartupInfo;
        private NativeTypes.PROCESS_INFORMATION ProcessInfo;

        private string LauncherKitPath;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            bool proceed = true;
            if (Permissions.IsAdministrator())
            {
                proceed = false;
                if (MessageBox.Show("For security reasons, explicitly running the Spore ModAPI Launcher as Administrator (by right-clicking and selecting \"Run as Administrator\") is not recommended. Doing so will also prevent you from being able to load creations into Spore by dragging their PNGs into the game window. Are you sure you want to proceed?", String.Empty, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    proceed = true;
            }

            if (proceed)
            {
                UpdateManager.CheckForUpdates();
                Application.EnableVisualStyles();

                // ensure we find Spore & GA as early as possible
                if (!SporePath.IsGameInstalled(true))
                {
                    return;
                }

                new Program().Execute();
            }
            else
            {
                Application.Exit();
            }
        }

        void Execute()
        {
            try
            {
                // store the launcher kit path for later
                this.LauncherKitPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                // We try a new approach for Steam users.
                // Before, we used Steam to launch the game and tried to find the new process and inject it.
                // However, when the injection happens the game already executed a bit, so mods fail.
                // Instead, we create a steam_appid.txt that allows us to execute SporeApp.exe directly                
                SporebinPath = SporePath.GetSporebinEP1Path();

                // use the default path for now (we might have to use a different one for Origin)
                this.ExecutablePath = Path.Combine(this.SporebinPath, "SporeApp.exe");
                this.ExecutableType = GameVersion.DetectVersion(this.ExecutablePath);

                // ensure we have detected a valid game version
                if (this.ExecutableType == GameVersionType.None)
                {
                    MessageBox.Show(Strings.UnsupportedSporeVersion, Strings.UnsupportedSporeVersionTitle);
                    return;
                }

                // check if SporeModLoader is installed
                try
                {
                    string sporeModLoaderPath = Path.Combine(this.SporebinPath, "dinput8.dll");
                    string originalFileName = "SporeModLoader.dll";
                    if (File.Exists(sporeModLoaderPath))
                    {
                        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(sporeModLoaderPath);
                        if (fileVersionInfo.InternalName == originalFileName ||
                            fileVersionInfo.OriginalFilename == originalFileName)
                        {
                            MessageBox.Show(Strings.SporeModLoaderDetected.Replace("$PATH$", sporeModLoaderPath), Strings.SporeModLoaderDetectedTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                catch (Exception)
                {
                    // ignore exception, SporeModLoader is likely not installed
                }

                // get the correct executable path
                this.ExecutablePath = Path.Combine(this.SporebinPath, GameVersion.GetExecutableFileName(this.ExecutableType));
                if (!File.Exists(this.ExecutablePath))
                {
                    // the file might only not exist in Origin (since Origin users will use a different executable compatible with ModAPI)
                    if (GameVersion.RequiresModAPIFix(this.ExecutableType))
                    {
                        if (!HandleOriginUsers())
                        {
                            return;
                        }
                    }
                    else
                    {
                        throw new Exception(CommonStrings.GameNotFound);
                    }
                }
                

                // we must also check if the steam_api.dll doesn't exist (it's required for Origin users)
                if (GameVersion.RequiresModAPIFix(this.ExecutableType) && !File.Exists(Path.Combine(this.SporebinPath, "steam_api.dll")))
                {
                    if (!HandleOriginUsers())
                    {
                        return;
                    }
                }

                if (SporePath.SporeIsInstalledOnSteam())
                {
                    string steamAppIdPath = Path.Combine(this.SporebinPath, "steam_appid.txt");
                    //we have to use Spore GAs appid now, due to steam DRM :(
                    if (!File.Exists(steamAppIdPath) || File.ReadAllText(steamAppIdPath) != SporePath.GalacticAdventuresSteamID)
                    {
                        try
                        {
                            File.WriteAllText(steamAppIdPath, SporePath.GalacticAdventuresSteamID);
                        }
                        catch
                        {
                            MessageBox.Show(Strings.CannotApplySteamFix.Replace("$PATH$", this.SporebinPath), Strings.CannotApplySteamFixTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }

                InjectSporeProcess(GameVersion.GetVersionName(this.ExecutableType));
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
        
        public void ShowError(Exception ex)
        {
            string versionInfo = "Launcher Kit version: " + UpdateManager.CurrentVersion + "\nModAPI DLLs version: " + UpdateManager.CurrentDllsBuild + "\nLauncher Kit path: " + Assembly.GetEntryAssembly().Location;
            if (this.ExecutablePath != null && File.Exists(this.ExecutablePath))
            {
                versionInfo += "\n\nSpore version: " + FileVersionInfo.GetVersionInfo(this.ExecutablePath).FileVersion + " - " + this.ExecutableType + "\nSpore path: " + this.ExecutablePath;
            }
            MessageBox.Show(Strings.GalacticAdventuresNotExecuted + "\n" + ModApiHelpThreadURL + "\n\n" + ex.GetType() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace + "\n\n" + versionInfo, CommonStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (ex is Win32Exception)
            {
                var exc = ex as Win32Exception;
                MessageBox.Show("ErrorCode: " + exc.ErrorCode + "\n" +
                                "NativeErrorCode: " + exc.NativeErrorCode + "\n" +
                                "HResult: " + exc.HResult + "\n", "Additional Win32Exception Error Info");

                if (exc.InnerException != null)
                {
                    MessageBox.Show("ErrorCode: " + exc.InnerException.GetType() + "\n\n" + exc.InnerException.Message + "\n\n" + exc.InnerException.StackTrace, "Win32Exception InnerException Error Info");
                }
            }
        }

        string[] GetDLLsToInject(string dllEnding)
        {
            List<string> dlls = new List<string>();

            //coreLibs and mLibs
            string coreLibsPath = Path.Combine(this.LauncherKitPath, "coreLibs");
            string mLibsPath    = Path.Combine(this.LauncherKitPath, "mLibs");

            if (!Directory.Exists(mLibsPath))
                Directory.CreateDirectory(mLibsPath);

            const string coreLibFile =  "SporeModAPI.lib";
            string coreLibPath = Path.Combine(coreLibsPath, coreLibFile);
            string coreLegacyDllName = "SporeModAPI-" + dllEnding + ".dll";
            string coreLegacyDllPath = Path.Combine(this.LauncherKitPath, coreLegacyDllName);
            string coreDllPath = Path.Combine(coreLibsPath, GameVersion.GetNewDLLName(this.ExecutableType));
            string coreDllOutPath = Path.Combine(mLibsPath, "SporeModAPI.dll");

            // ensure ModAPI DLLs exist
            foreach (string dll in new string[] { coreLegacyDllPath, coreDllPath })
            {
                if (!File.Exists(dll))
                {
                    throw new FileNotFoundException("Required ModAPI DLL was not found: " + dll);
                }
            }

            File.Copy(coreLibPath, Path.Combine(mLibsPath, coreLibFile), true);
            File.Copy(coreDllPath, coreDllOutPath, true);

            dlls.Add(coreDllOutPath);
            dlls.Add(coreLegacyDllPath);

            foreach (string file in Directory.EnumerateFiles(mLibsPath)
                                          .Where(x => 
                                          {
                                              x = x.ToLowerInvariant();
                                              return x.EndsWith(".dll") && x != coreDllOutPath.ToLowerInvariant();
                                          }))
            {
                dlls.Add(file);
            }

            foreach (string file in Directory.EnumerateFiles(this.LauncherKitPath, "*" + dllEnding + ".dll")
                                            .Where(x => x.ToLowerInvariant() != coreLegacyDllPath.ToLowerInvariant()))
            {
                dlls.Add(file);
            }

            return dlls.ToArray();
        }

        void InjectSporeProcess(string dllEnding)
        {
            CreateSporeProcess();

            try
            {
                string modApiDllInjectorPath = Path.Combine(this.LauncherKitPath, "ModAPI.DLLInjector.dll");
                if (!File.Exists(modApiDllInjectorPath))
                {
                    throw new FileNotFoundException("Required injector DLL was not found: " + modApiDllInjectorPath);
                }

                IntPtr hDLLInjectorHandle = Injector.InjectDLL(this.ProcessInfo, modApiDllInjectorPath);
                if (hDLLInjectorHandle == IntPtr.Zero)
                {
                    throw new Win32Exception("Failed to inject DLL: " + modApiDllInjectorPath);
                }

                string[] dlls = GetDLLsToInject(dllEnding);

                Injector.SetInjectionData(this.ProcessInfo, hDLLInjectorHandle, dllEnding == "disk", dlls);

                if (NativeMethods.IsDebuggerPresent())
                {
                    DTE2 dte = DebugHelper.GetActiveDebugger();
                    if (dte != null)
                    {
                        foreach (var proc in dte.Debugger.LocalProcesses.Cast<EnvDTE.Process>().Where(proc => proc.ProcessID == ProcessInfo.dwProcessId))
                        {
                            ResumeSporeProcess();
                            proc.Attach();
                            return;
                        }
                    }
                }

                ResumeSporeProcess();
            }
            catch (Exception e)
            {
                // always terminate suspended Spore process on failure
                NativeMethods.TerminateProcess(this.ProcessInfo.hProcess, 0);
                throw e;
            }
        }

        void CreateSporeProcess()
        {
            var sb = new StringBuilder();
            // skip first argument because it's the launcher executable
            foreach (string arg in Environment.GetCommandLineArgs().Skip(1))
            {
                sb.Append(arg);
                sb.Append(" ");
            }

            if (!NativeMethods.CreateProcess(null, "\"" + this.ExecutablePath + "\" " + sb,
                    IntPtr.Zero, IntPtr.Zero, false, NativeTypes.ProcessCreationFlags.CREATE_SUSPENDED, IntPtr.Zero, 
                    this.SporebinPath, ref this.StartupInfo, out this.ProcessInfo))
            {
                ThrowWin32Exception(Strings.ProcessNotStarted);
            }
        }

        void ResumeSporeProcess()
        {
            if (NativeMethods.ResumeThread(this.ProcessInfo.hThread) != 1)
            {
                ThrowWin32Exception(Strings.ProcessNotResumed);
            }
        }

        bool HandleOriginUsers()
        {
            if (MessageBox.Show(Strings.DownloadOriginFix, Strings.FileNeeded, MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                return ShowDownloadFixDialog(this.SporebinPath);
            }
            else
            {
                // don't execute the game
                return false;
            }
        }

        // -- DIALOGS -- //

        static bool ShowDownloadFixDialog(string outputPath)
        {
            bool result = false;

            Thread thread = new Thread(() =>
            {
                var dialog = new ProgressDialog(Strings.DownloadFixTitle, Strings.DownloadFixTitle, (s, e) =>
                {
                    try
                    {
                        MemoryStream memoryStream;

                        using (var downloadClient = new DownloadClient(ModAPIFixDownloadURL))
                        {
                            downloadClient.DownloadProgressChanged += (_, progress) =>
                            {
                                (s as BackgroundWorker).ReportProgress(progress);
                            };

                            downloadClient.SetTimeout(TimeSpan.FromMinutes(5));
                            memoryStream = downloadClient.DownloadToMemory();
                        }

                        result = ExtractFixFiles(memoryStream, outputPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Failed to download or extract ModAPI Fix");
                        result = false;
                    }
                });

                dialog.ShowDialog();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return result;
        }

        // -- UTILITY METHODS -- //

        static bool ExtractFixFiles(MemoryStream zipStream, string outputPath)
        {
            try
            {
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        entry.ExtractToFile(Path.Combine(outputPath, entry.Name), true);
                    }
                }
                return true;
            }
            catch (System.UnauthorizedAccessException)
            {
                MessageBox.Show(CommonStrings.UnauthorizedAccess, CommonStrings.UnauthorizedAccessTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static void ThrowWin32Exception(string title, string additionalErrorText = "")
        {
            int error = Marshal.GetLastWin32Error();
            MessageBox.Show("Error: " + error + additionalErrorText, title);
            throw new Win32Exception(error);
        }
    }
}
