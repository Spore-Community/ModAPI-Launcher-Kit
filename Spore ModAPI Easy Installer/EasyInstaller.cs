using ModApi.Common;
using ModAPI.Common;
using ModAPI.Common.Dialog;
using ModAPI.Common.Types;
using ModAPI.Common.Update;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace Spore_ModAPI_Easy_Installer
{
    public static class EasyInstaller
    {
        enum FileChooserType
        {
            File,
            Directory
        }

        enum FileType
        {
            None,  // none of the supported types
            EXE,
            DLL,
            SporeMod,  // just a .zip renamed to .sporemod
            Package,
            Spore_Package // a .package that goes to the Spore data folder instead of the EP1 one
        }

        //// Show a file chooser and returns the path selected. It can ask for files or directories.
        //static string ShowFileChooser(FileChooserType type, string title, string filter);

        //// Returns the path of the file that must be installed. It can get it from the command line or from a file chooser dialog.
        //static string GetInputPath();
        ///* -- if no arguments have been provided to the .exe, call ShowFileChooser(FileChooserType.Directory) */

        //// Determines which folder the file goes to depending on its type
        //static string GetOutputPath(FileType type);

        //// Determines what kind of file is the argument given based on the extension
        //static FileType GetFileType(string fileName);

        // 

        public static InstalledMods ModList = new InstalledMods();
        public static string outcome = string.Empty;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
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
                LauncherSettings.Load();
                ModList.Load();

                // ensure we find Spore & GA as early as possible
                if (PathDialogs.ProcessSpore() == null ||
                    PathDialogs.ProcessGalacticAdventures() == null)
                {
                    return;
                }

                var cmdArgs = Environment.GetCommandLineArgs();
                if ((cmdArgs.Length == 4) && bool.TryParse(cmdArgs[2], out bool configResult) && bool.TryParse(cmdArgs[3], out bool uninstall))
                {
                    string modName = cmdArgs[1];

                    if (configResult)
                    {
                        Thread thread = GetXmlInstaller(modName, configResult, uninstall, true, out XmlInstallerWindow win);
                        thread.Join();
                    }
                }
                else
                {
                    // ask the user for mod files using a file dialog
                    string[] inputPaths = ShowFileChooser(FileChooserType.File, Strings.FileChooserTitle,
                                                            Strings.FileChooserFilter, 4);
                    string[] errorStrings = new string[inputPaths.Length];
                    if (inputPaths.Length < 1) return;

                    List<ResultType> results = new List<ResultType>();
                    // 2nd: Check what kind of input we got, and proceed to install it
                    for (int i = 0; i < inputPaths.Length; i++)
                    {
                        string inputPath = inputPaths[i];
                        FileType fileType = GetFileType(Path.GetFileName(inputPath));
                        string modName = Path.GetFileNameWithoutExtension(inputPath);
                        ResultType result = ResultType.UnsupportedFile;


                        try
                        {
                            switch (fileType)
                            {
                                case FileType.Package:
                                    // install the package normally
                                    result = InstallPackage(inputPath, modName);
                                    // add to installed mods list
                                    ModList.AddMod(modName).AddFile(Path.GetFileName(inputPath), SporePath.Game.GalacticAdventures);
                                    break;

                                case FileType.SporeMod:
                                    // first, check if there is an installer
                                    // if not, put every file in the ZIP to the corresponding place
                                    // and add to installed mods list
                                    result = InstallSporemod(inputPath, modName);
                                    break;

                                default:
                                    result = ResultType.UnsupportedFile;
                                    break;
                            }
                            results.Add(result);
                        }
                        catch (Exception ex)
                        {
                            errorStrings[i] = ex.Message;
                            results.Add(ResultType.UnsupportedFile);
                        }
                    }
                    for (int i = 0; i < results.Count; i++)
                    {
                        ResultType type = results[i];
                        outcome += GetResultText(type, Path.GetFileNameWithoutExtension(inputPaths[i]), errorStrings[i]) + "\n";
                    }

                    ModList.Save();
                    ShowExitMessageBox();
                }
            }
        }
        static string[] ShowFileChooser(FileChooserType type, string title, string filter, int filterIndex)
        {
            string[] paths = new string[0];
            Thread thread = new Thread(() =>
            {
                if (type == FileChooserType.File)
                {
                    var dialog = new OpenFileDialog()
                    {
                        Title = title,
                        Filter = filter,
                        FilterIndex = filterIndex,
                        Multiselect = true
                    };
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        paths = dialog.FileNames;
                    }
                }
                else
                {
                    var dialog = new FolderBrowserDialog();
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        paths = new string[] { dialog.SelectedPath };
                    }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return paths;
        }

        static FileType GetFileType(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                return FileType.None;
            }

            fileName = fileName.ToLowerInvariant();

            if (fileName.EndsWith(".package"))
            {
                return FileType.Package;
            }
            else if (fileName.EndsWith(".dll"))
            {
                return FileType.DLL;
            }
            else if (fileName.EndsWith(".sporemod"))
            {
                return FileType.SporeMod;
            }
            else
            {
                return FileType.None;
            }
        }

        static string GetOutputPath(FileType type)
        {
            switch (type)
            {
                case FileType.DLL:
                    return Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString();

                case FileType.Package:
                    return GetGADataPath();

                case FileType.Spore_Package:
                    return GetSporeDataPath();

                default:
                    return null;
            }
        }

        // this takes a "pathType" from an InstalledFile
        static string GetOutputPath(string pathType)
        {
            switch (pathType)
            {
                case "GalacticAdventures":
                    return GetOutputPath(FileType.Package);

                case "Spore":
                    return GetOutputPath(FileType.Spore_Package);

                case "None":
                    // we use "None" for dlls
                    return GetOutputPath(FileType.DLL);

                default:
                    return null;
            }
        }

        static string GetGADataPath()
        {
            string path = PathDialogs.ProcessGalacticAdventures();

            if (path != null)
            {
                // now we have the path to SporebinEP1; move it to Data
                path = SporePath.MoveToData(SporePath.Game.GalacticAdventures, SporePath.GetRealParent(path));
            }

            return path;
        }

        static string GetSporeDataPath()
        {
            string path = PathDialogs.ProcessSpore();

            if (path != null)
            {
                // now we have the path to Sporebin; move it to Data
                path = SporePath.MoveToData(SporePath.Game.Spore, SporePath.GetRealParent(path));
            }

            return path;
        }

        static ResultType InstallPackage(string inputFile, string modName)
        {
            ResultType result = ResultType.Success;
            Exception ex = null;

            string outputPath = GetOutputPath(FileType.Package);
            if (outputPath == null)
            {
                return ResultType.GalacticAdventuresNotFound;
            }

            try
            {
                string fileName = Path.GetFileName(inputFile);
                string outputFile = Path.Combine(outputPath, fileName);

                Thread thread = new Thread(() =>
                {
                    var dialog = new ProgressDialog(Strings.CopyingFile + " " + fileName, Strings.InstallingModTitle, (s, e) =>
                    {
                        try
                        {
                            using (FileStream inputFileStream = File.Open(inputFile, FileMode.Open))
                            using (FileStream outputFileStream = File.Open(outputFile, FileMode.Create))
                            {
                                StreamUtils.CopyStreamWithProgress(inputFileStream, outputFileStream, null, (_, progress) =>
                                {
                                    (s as BackgroundWorker).ReportProgress(progress);
                                });

                                outputFileStream.Flush();
                            }
                        }
                        catch (Exception copyException)
                        {
                            ex = copyException;
                            result = ResultType.ModNotInstalled;
                        }
                    });

                    dialog.ShowDialog();
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                if (ex != null)
                {
                    throw ex;
                }

                return result;
            }
            catch (UnauthorizedAccessException)
            {
                return ResultType.UnauthorizedAccess;
            }
        }

        static SporePath.Game GetGameFromFileType(FileType type)
        {
            switch (type)
            {
                case FileType.Package:
                    return SporePath.Game.GalacticAdventures;

                case FileType.Spore_Package:
                    return SporePath.Game.Spore;

                default:
                    return SporePath.Game.None;
            }
        }

        // Installs the files and adds them to the list in the ModConfiguration (so they can be removed if something goes wrong)
        private static ResultType ExtractSporemodZip(string inputFile, ModConfiguration mod, StreamUtils.StreamProgressEventHandler eventHandler)
        {
            //TODO check if it contains an installer
            string modName = Path.GetFileNameWithoutExtension(inputFile);
            using (ZipArchive archive = ZipFile.Open(inputFile, ZipArchiveMode.Read))
            {
                int numEntries = archive.Entries.Count;
                int entriesExtracted = 0;
                string configsPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "ModConfigs");
                if (!Directory.Exists(configsPath))
                    Directory.CreateDirectory(configsPath);
                string modPath = Path.Combine(configsPath, modName);
                if (!Directory.Exists(modPath))
                    Directory.CreateDirectory(modPath);

                foreach (var entry in archive.Entries)
                {
                    // we use the FullName because we also might check the folder that contains that file
                    var type = GetFileType(entry.FullName);
                    string outputPath = GetOutputPath(type);
                    if (entry.FullName.Contains("."))
                    {
                        mod.AddFile(entry.Name, GetGameFromFileType(type));
                        string configOutPath = Path.Combine(modPath, entry.Name);

                        entry.ExtractToFile(configOutPath, true);
                        if (outputPath != null)
                        {
                            string fileOutPath = Path.Combine(outputPath, entry.Name);

                            File.Copy(configOutPath, fileOutPath, true);
                        }
                    }

                    eventHandler?.Invoke(null, (int)((entriesExtracted / (float)numEntries) * 100.0f));
                    entriesExtracted++;
                }
            }

            return ResultType.Success;
        }

        private static Version GetModCoreDllsVersion(ZipArchiveEntry xmlEntry)
        {
            using (var stream = xmlEntry.Open())
            {
                var document = new XmlDocument();
                document.Load(stream);

                var modNode = document.SelectSingleNode("/mod");
                if (modNode != null && modNode.Attributes["dllsBuild"] != null)
                {
                    return Version.Parse(modNode.Attributes["dllsBuild"].Value);
                }
            }
            return null;
        }

        static ResultType TryExecuteInstaller(string inputFile, string modName)
        {
            using (ZipArchive archive = ZipFile.Open(inputFile, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry("Installer.exe");
                var xmlEntry = archive.GetEntry("ModInfo.xml");


                if (xmlEntry != null)
                {
                    Version modCoreDllsVersion = null;
                    try
                    {
                        modCoreDllsVersion = GetModCoreDllsVersion(xmlEntry);
                    }
                    // If the version cannot be read due to an exception, show an error and don't install the mod
                    catch
                    {
                        MessageBox.Show($"\"{modName}\"{Strings.InvalidDllVersion}", Strings.InvalidDllVersionTitle);
                        return ResultType.ModNotInstalled;
                    }
                    // If the version can be read but is outdated, show an error and don't install the mod
                    if (modCoreDllsVersion != null && modCoreDllsVersion > UpdateManager.CurrentDllsBuild)
                    {
                        MessageBox.Show($"\"{modName}\"{Strings.OutdatedDllVersion.Replace("$REQUIREDVERSION$", modCoreDllsVersion.ToString()).Replace("$CURRENTVERSION$", UpdateManager.CurrentDllsBuild.ToString())}", Strings.OutdatedDllVersionTitle);
                        return ResultType.ModNotInstalled;
                    }
                    // If the version is not specified, continue installing (the value is optional because not all mods use the ModAPI SDK)

                    string modPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "ModConfigs", modName);
                    if (Directory.Exists(modPath))
                        DeleteFolder(modPath);

                    Directory.CreateDirectory(modPath);


                    Thread installerThread = GetXmlInstaller(modName, false, false, true, out XmlInstallerWindow win);

                    foreach (var fileEntry in archive.Entries)
                    {
                        fileEntry.ExtractToFile(Path.Combine(modPath, fileEntry.Name), true);
                    }

                    win.SignalRevealInstaller();

                    installerThread.Join();

                    if (!XmlInstallerCancellation.Cancellation[modName.Trim('"')])
                        return win.GetResult();
                    else
                        return ResultType.ModNotInstalled;
                }
                else if (entry != null)
                {
                    return ResultType.UnsupportedFile;
                }
                else
                {
                    return ResultType.NoInstallerFound;
                }
            }
        }

        public static void DeleteFolder(string path)
        {
            foreach (string s in Directory.EnumerateFiles(path))
                File.Delete(s);

            foreach (string s in Directory.EnumerateDirectories(path))
                DeleteFolder(s);

            Directory.Delete(path);
        }

        static Thread GetXmlInstaller(string modName, bool configure, bool uninstall, bool show, out XmlInstallerWindow win)
        {
            XmlInstallerWindow xmlWin = null;
            Thread thread = new Thread(() =>
            {
                xmlWin = new XmlInstallerWindow(modName, configure, uninstall);
                if (show)
                {
                    xmlWin.ShowDialog();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            // wait until xmlWin has been set
            while (xmlWin == null)
            {
                Thread.Sleep(10);
            }

            win = xmlWin;
            return thread;
        }

        static ResultType InstallSporemod(string inputFile, string modName)
        {
            var result = TryExecuteInstaller(inputFile, modName);

            if (result == ResultType.NoInstallerFound)
            {
                // the custom installer just didn't exist, extract as ZIP file
                var mod = ModList.AddMod(modName);
                Exception exception = null;

                Thread thread = new Thread(() =>
                {
                    var dialog = new ProgressDialog(Strings.ModIsInstalling1 + modName + "\" is being installed", Strings.InstallingModTitle, (s, e) =>
                    {
                        try
                        {
                            ExtractSporemodZip(inputFile, mod, (_, progress) =>
                            {
                                (s as BackgroundWorker).ReportProgress(progress);
                            });

                            result = ResultType.Success;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // remove all the files we added (so the mod is not only partially installed)
                            RemoveModFiles(mod);
                            ModList.RemoveMod(mod);

                            result = ResultType.UnauthorizedAccess;
                        }
                        catch (IOException)
                        {
                            // remove all the files we added (so the mod is not only partially installed)
                            RemoveModFiles(mod);
                            ModList.RemoveMod(mod);
                            result = ResultType.InvalidPath;
                        }
                        catch (Exception ex)
                        {
                            // remove all the files we added (so the mod is not only partially installed)
                            RemoveModFiles(mod);
                            ModList.RemoveMod(mod);

                            // just propagate the exception
                            exception = ex;
                        }
                    });

                    dialog.ShowDialog();
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                if (exception != null)
                {
                    throw exception;
                }

                return result;
            }
            else
            {
                // the Installer existed but there was a problem
                return result;
            }

        }

        static void RemoveModFiles(ModConfiguration mod)
        {
            foreach (InstalledFile file in mod.InstalledFiles)
            {
                string outputPath = GetOutputPath(file.PathType);

                if (outputPath != null)
                {
                    try
                    {
                        File.Delete(Path.Combine(outputPath, file.Name));
                    }
                    catch
                    {
                        // just continue
                    }
                }
            }
        }


        static string GetErrorMessage(ResultType errorType)
        {
            switch (errorType)
            {
                case ResultType.UnsupportedFile: return Strings.ErrorUnsupportedFile;
                case ResultType.GalacticAdventuresNotFound: return CommonStrings.GalacticAdventuresNotFound;
                case ResultType.UnauthorizedAccess: return CommonStrings.UnauthorizedAccess;
                case ResultType.InvalidPath: return CommonStrings.InvalidPath;

                default:
                    return null;
            }
        }


        // this one does not block the thread
        static string GetResultText(ResultType result, string modName, string errorString)
        {
            if (result == ResultType.Success)
            {
                // show message to the user
                return Strings.ModInstalled1 + modName + Strings.ModInstalled2;
            }
            else if (result == ResultType.ModNotInstalled)
            {
                return Strings.ModInstalled1 + modName + Strings.CancelledInstallation;
            }
            else
            {
                if (errorString == null)
                {
                    errorString = GetErrorMessage(result);
                }
                // show message to the user
                //MessageBox.Show(Strings.ModNotInstalled1 + modName + Strings.ModNotInstalled2 + " " + errorString, Strings.InstallationCancelled);
                return Strings.ModNotInstalled1 + modName + Strings.ModNotInstalled2 + " " + errorString;
            }
        }

        static void ShowExitMessageBox()
        {
            Thread thread = new Thread(() =>
            {
                var win = ModInstalledWindow.GetDialog(outcome, Strings.InstallationCompleted); //"Your selected mods are done installing.\n" + 
                win.Closed += (snedre, rags) => Process.GetCurrentProcess().Kill();
                win.ShowDialog();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
