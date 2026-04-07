using ModAPI.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using ModAPI.Common.Types;
using ModAPI.Common.Update;
using ModAPI.Common.UI;
using ModAPI.Common.UI.Windows;

namespace Spore_ModAPI_Easy_Installer
{
    public static class XmlInstallerCancellation
    {
        public static Dictionary<string, bool> Cancellation = new Dictionary<string, bool>();
    }

    /// <summary>
    /// Interaction logic for XmlInstallerWindow.xaml
    /// </summary>
    public partial class XmlInstallerWindow : DecoratableWindow
    {
        public static string GameSporeString = "Spore";
        public static string GameGaString = "GalacticAdventures";

        public static List<XmlInstallerWindow> installerWindows = new List<XmlInstallerWindow>();
        string ModSettingsStoragePath = string.Empty;
        string ModConfigPath = string.Empty;
        string ModUnique = string.Empty;
        Version ModVersion = null;
        List<string> ModDependencies = new List<string>();
        List<Version> ModDependenciesVersions = new List<Version>();
        XmlDocument Document = new XmlDocument();
        List<ComponentInfo> Prerequisites = new List<ComponentInfo>();
        List<ComponentInfo> CompatFiles = new List<ComponentInfo>();
        List<ComponentInfo> InstalledCompatFiles = new List<ComponentInfo>();
        List<RemovalInfo> Removals = new List<RemovalInfo>();
        string ModDisplayName = string.Empty;
        string ModName = string.Empty;
        string ModDescription = string.Empty;
        bool _isConfigurator = false;
        bool _isUninstallingOnly = false;
        int _activeComponentCount = 0;
        CircleEase _ease = new CircleEase()
        {
            EasingMode = EasingMode.EaseOut
        };
        TimeSpan _time = TimeSpan.FromMilliseconds(250);
        Version ModInfoVersion = null;// new Version(1, 0, 0, 0);
        int _installerState = 0; //0 = waiting, 1 = installing
        int _installerMode = 0; //0 = show components list, 1 = don't show components list
        bool _isModMatched = false;
        bool _dontUseLegacyPackagePlacement = false;
        public static string GaDataPath = SporePath.MoveToData(SporePath.Game.GalacticAdventures, SporePath.GetRealParent(PathDialogs.ProcessGalacticAdventures()));

        public static string SporeDataPath = SporePath.MoveToData(SporePath.Game.Spore, SporePath.GetRealParent(PathDialogs.ProcessSpore()));

        private ResultType _result = ResultType.ModNotInstalled;

        public XmlInstallerWindow(string modName, bool configure, bool uninstall)
        {
            InitializeComponent();
            //LauncherSettings.Load();
            installerWindows.Add(this);

            if (!Directory.Exists(SporeDataPath))
                PathDialogs.ProcessSpore();
            if (!Directory.Exists(GaDataPath))
                PathDialogs.ProcessGalacticAdventures();

            ModName = modName.Trim('"');
            XmlInstallerCancellation.Cancellation.Add(ModName, false);
            ModConfigPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "ModConfigs", ModName); // Settings.ProgramDataPath + @"\ModConfig\" + modName + @"\";
            //if (!Directory.Exists())
            _isConfigurator = configure;
            _isUninstallingOnly = uninstall;
            if (_isConfigurator)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                RevealInstaller();

                if (_isUninstallingOnly)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        StartUninstallationButton_Click(StartUninstallationButton, null);
                    }));
                }
            }
        }

        public ResultType GetResult()
        {
            return _result;
        }

        public void SignalRevealInstaller()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                RevealInstaller();
            }));
        }

        public void RevealInstaller()
        {
            if (!_isConfigurator)
            {
                Show();
                Focus();
                Activate();

                DoubleAnimation FadeOutAnimation = new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    From = 1,
                    To = 0
                };
                FadeOutAnimation.Completed += (sneder, args) => LoadingPanel.Visibility = Visibility.Collapsed;
                LoadingPanel.BeginAnimation(Border.OpacityProperty, FadeOutAnimation);
            }

            try
            {
                /*if (!Directory.Exists(ModConfigPath))
                    Directory.CreateDirectory(ModConfigPath);*/
                //ModName = fixedName;
                ModDisplayName = ModName;
                Document.Load(Path.Combine(ModConfigPath, "ModInfo.xml"));

                PrepareInstaller();
            }
            catch (Exception ex)
            {
                ErrorInfoTextBlock.Text = ex.ToString();
                CyclePage(0, 3);
            }
        }

        private void PrepareInstaller()
        {
            try
            {
                ModInfoVersion = Version.Parse(Document.SelectSingleNode("/mod").Attributes["installerSystemVersion"].Value);

                if ((ModInfoVersion == new Version(1, 0, 0, 0)) || 
                    (ModInfoVersion == new Version(1, 0, 1, 0)) || 
                    (ModInfoVersion == new Version(1, 0, 1, 1)) ||
                    (ModInfoVersion == new Version(1, 0, 1, 2)) ||
                    (ModInfoVersion == new Version(1, 0, 1, 3)))
                {
                    if (File.Exists(Path.Combine(ModConfigPath, "Branding.png")))
                    {
                        (TitleBarContent as Grid).Visibility = Visibility.Visible;
                        BitmapImage brandingImage = new BitmapImage();
                        using (MemoryStream byteStream = new MemoryStream(File.ReadAllBytes(Path.Combine(ModConfigPath, "Branding.png"))))
                        {
                            brandingImage.BeginInit();
                            brandingImage.CacheOption = BitmapCacheOption.OnLoad;
                            brandingImage.StreamSource = byteStream;
                            brandingImage.EndInit();
                        }
                        BorderThickness = new Thickness(BorderThickness.Left, 60, BorderThickness.Right, BorderThickness.Bottom);
                        BrandingCanvas.Width = brandingImage.PixelWidth;
                        BrandingCanvas.Source = brandingImage;
                        ShowTitlebarText = false;
                    }
                    else
                        Title = ModDisplayName + " Installer";

                    if ((ModInfoVersion == new Version(1, 0, 0, 0)) && (Document.SelectSingleNode("/mod").Attributes["mode"] != null))
                    {
                        if (Document.SelectSingleNode("/mod").Attributes["mode"].Value == "compatOnly")
                            _installerMode = 1;
                    }
                    else if ((ModInfoVersion == new Version(1, 0, 1, 0)) || 
                             (ModInfoVersion == new Version(1, 0, 1, 1)) ||
                             (ModInfoVersion == new Version(1, 0, 1, 2)) ||
                             (ModInfoVersion == new Version(1, 0, 1, 3)))
                    {
                        _dontUseLegacyPackagePlacement = true;
                        _installerMode = 1;
                        if (Document.SelectSingleNode("/mod").Attributes["hasCustomInstaller"] != null)
                        {
                            if (bool.TryParse(Document.SelectSingleNode("/mod").Attributes["hasCustomInstaller"].Value, out bool hasCustomInstaller) && hasCustomInstaller)
                            {
                                _installerMode = 0;
                            }
                        }

                        if ((Document.SelectSingleNode("/mod").Attributes["dllsBuild"] != null) && (Version.TryParse(Document.SelectSingleNode("/mod").Attributes["dllsBuild"].Value + ".0", out Version minFeaturesLevel)))
                        {
                            if (minFeaturesLevel > UpdateManager.CurrentDllsBuild)
                                CyclePage(0, 2);
                        }
                        else
                            throw new Exception("This Mod has not specified a valid minimum features level. Please inform the mod's developer of this.");
                    }

                    if (Document.SelectSingleNode("/mod").Attributes["unique"].Value != null)
                        ModUnique = Document.SelectSingleNode("/mod").Attributes["unique"].Value;
                    else
                        throw new Exception("This Mod's installer does not have a Unique Identifier. Please inform the mod's developer of this.");

                    ModConfiguration[] configs = EasyInstaller.ModList.ModConfigurations.ToArray();
                    
                    foreach (ModConfiguration mod in configs)
                    {
                        if (mod.Unique == ModUnique)
                        {
                            if (mod.Name == ModName)
                            {
                                _isModMatched = true;
                                break;
                            }
                        }
                    }

                    if (_isConfigurator || (!_isModMatched))
                    {
                        string displayName = string.Empty;

                        if (Document.SelectSingleNode("/mod").Attributes["displayName"] != null)
                        {
                            displayName = Document.SelectSingleNode("/mod").Attributes["displayName"].Value;
                            ModDisplayName = displayName;
                        }
                        else
                        {
                            ModDisplayName = ModUnique;
                        }
                        Title = ModDisplayName + " Installer";


                        if (Document.SelectSingleNode("/mod").Attributes["description"] != null)
                            ModDescription = Document.SelectSingleNode("/mod").Attributes["description"].Value;
                        else
                            ModDescription = string.Empty;

                        // installerSystemVersion 1.0.1.3 introduced the version, depends and dependsVersion attributes
                        if (ModInfoVersion == new Version(1, 0, 1, 3))
                        {
                            if (Document.SelectSingleNode("/mod").Attributes["version"] != null)
                            {
                                if (Version.TryParse(Document.SelectSingleNode("/mod").Attributes["version"].Value, out Version parsedVersion))
                                    ModVersion = parsedVersion;
                                else
                                    throw new Exception("This mod has an invalid version. Please inform the mod's developer of this.");
                            }
                            else
                                ModVersion = null;

                            if (Document.SelectSingleNode("/mod").Attributes["depends"] != null)
                            {
                                string dependsString = Document.SelectSingleNode("/mod").Attributes["depends"].Value;
                                string dependsVersionString = null;
                                if (Document.SelectSingleNode("/mod").Attributes["dependsVersions"] != null)
                                    dependsVersionString = Document.SelectSingleNode("/mod").Attributes["dependsVersions"].Value;
                                string[] dependsUniqueNames = dependsString.Split('?');
                                string[] dependsVersions = dependsVersionString?.Split('?');
                                bool[] foundDepends = new bool[dependsUniqueNames.Length];

                                ModDependencies = dependsUniqueNames.ToList();
                                if (dependsVersions != null)
                                {
                                    foreach (string dependsVersion in dependsVersions)
                                    {
                                        if (String.IsNullOrEmpty(dependsVersion))
                                        { // 0.0.0.0 is interpreted as having no version attribute
                                          // but we need to pad the list to match the amount of items
                                          // that ModDependencies has
                                            ModDependenciesVersions.Add(new Version(0, 0, 0, 0));
                                        }
                                        else
                                        {
                                            if (Version.TryParse(dependsVersion, out Version parsedDependsVersion))
                                            {
                                                ModDependenciesVersions.Add(parsedDependsVersion);
                                            }
                                            else
                                            {
                                                throw new Exception($"This mod has an invalid dependsVersions entry: \"{dependsVersion}\". Please inform the mod's developer of this.");
                                            }
                                        }
                                    }
                                }

                                foreach (ModConfiguration mod in configs)
                                {
                                    for (int i = 0; i < dependsUniqueNames.Length; i++)
                                    {
                                        if (mod.Unique == dependsUniqueNames[i])
                                        {
                                            if (mod.Version != null &&
                                                (ModDependenciesVersions.Count() > i) &&
                                                (mod.Version >= ModDependenciesVersions[i]))
                                            { // name + version match
                                                foundDepends[i] = true;
                                                break;
                                            }
                                            else if (ModDependenciesVersions.Count() <= i ||
                                                     ModDependenciesVersions[i] == new Version(0, 0, 0, 0))
                                            { // name only match
                                                foundDepends[i] = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                                bool satisfiedDepends = true;
                                List<string> missingDepends = new List<string>();
                                for (int i = 0; i < foundDepends.Length; i++)
                                {
                                    if (!foundDepends[i])
                                    {
                                        satisfiedDepends = false;
                                        string missingDependsText = dependsUniqueNames[i];
                                        if (dependsVersions != null &&
                                            dependsVersions.Length > i)
                                        {
                                            missingDependsText += " " + dependsVersions[i];
                                        }
                                        missingDepends.Add(missingDependsText);
                                    }
                                }

                                if (!satisfiedDepends)
                                {
                                    throw new Exception($"This mod has dependencies that you don't have installed: {String.Join(", ", missingDepends)}");
                                }
                            }
                        }

                        NameTextBlock.Text = displayName;
                        DescriptionTextBlock.Text = ModDescription;

                        ModSettingsStoragePath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "ModSettings", ModUnique);
                        string settingsStorageDir = Path.GetDirectoryName(ModSettingsStoragePath);
                        //Debug.WriteLine("ModSettingsStoragePath: " + ModSettingsStoragePath);
                        //Debug.WriteLine("settingsStorageDir: " + settingsStorageDir);

                        if (!Directory.Exists(settingsStorageDir))
                            Directory.CreateDirectory(settingsStorageDir);

                        GetComponents();

                        if (_isConfigurator)
                        {
                            StartInstallationButton.Content = "Apply";
                            StartUninstallationButton.Visibility = Visibility.Visible;
                        }

                        /*if (!ShowProtectiveDialogs())
                            return;*/
                        if (_installerMode == 1)
                        {
                            //if (_isConfigurator)
                            StartInstallationButton_Click(StartInstallationButton, null);
                            /*else
                                StartUninstallationButton_Click(StartUninstallationButton, null);*/
                        }
                    }
                    else
                    {
                        InstallationCompleteHeaderTextBlock.Text = "You already have this mod installed!";
                        InstallationCompleteDescriptionTextBlock.Text = "To change the mod's settings or uninstall it, go to the ModAPI Easy Uninstaller.";

                        CyclePage(0, 1);
                    }
                }
                else
                {
                    CyclePage(0, 2);
                }
            }
            catch (Exception ex)
            {
                ErrorInfoTextBlock.Text = ex.ToString();
                CyclePage(0, 3);
            }
        }

        private void GetComponents()
        {
            foreach (XmlNode node in Document.SelectSingleNode("/mod").ChildNodes)
            {
                Debug.WriteLine("/mod");
                if (node.Name.ToLower() == "component")
                {
                    Debug.WriteLine("component");
                    ComponentInfo info = GetComponentFromXmlNode(node, false, true);
                    CheckBox checkBox = new CheckBox()
                    {
                        Content = info.ComponentDisplayName,
                        Tag = info
                    };
                    checkBox.MouseEnter += Component_MouseEnter;
                    checkBox.MouseLeave += Component_MouseLeave;

                    checkBox.IsChecked = false;

                    string storedValue = GetSettingValueFromStorageString(info.ComponentUniqueName);
                    if (storedValue != null && bool.TryParse(storedValue, out bool valueBool))
                        checkBox.IsChecked = valueBool;
                    else
                        checkBox.IsChecked = info.defaultChecked;

                    ComponentListStackPanel.Children.Add(checkBox);
                }
                else if (node.Name.ToLower() == "componentgroup")
                {
                    bool hasSelection = false;
                    Debug.WriteLine("componentGroup");
                    string groupDisplayName = node.Attributes["displayName"].Value;
                    string groupUnique = node.Attributes["unique"].Value;
                    if (!IsUniqueNameValid(groupUnique))
                        throw new Exception("Invalid unique name: " + groupUnique);
                    //if (string.IsNullOrWhiteSpace(groupUnique))
                    GroupBox componentRadioGroupBox = new GroupBox()
                    {
                        Header = groupDisplayName,
                        Tag = groupUnique,
                        Content = new StackPanel()
                        {
                            Orientation = Orientation.Vertical
                        }
                    };

                    var componentRadioStackPanel = componentRadioGroupBox.Content as StackPanel;

                    string storedValue = GetSettingValueFromStorageString(groupUnique);

                    foreach (XmlNode subNode in node.ChildNodes)
                    {
                        ComponentInfo info = GetComponentFromXmlNode(subNode, false, true);
                        RadioButton radioButton = new RadioButton()
                        {
                            Content = info.ComponentDisplayName,
                            Tag = info,
                            GroupName = groupDisplayName
                        };
                        radioButton.MouseEnter += Component_MouseEnter;
                        radioButton.MouseLeave += Component_MouseLeave;
                        if /*(info.defaultChecked & */(!hasSelection)//)
                        {
                            //radioButton.IsChecked = true;
                            radioButton.IsChecked = false;
                            //if (_isConfigurator && (enabledList.Contains(ComponentInfoToQuestionMarkSeparatedString(info)))) //info.ComponentFileName + "?" + info.ComponentGame)))
                            if (_isConfigurator)
                                radioButton.IsChecked = (storedValue == info.ComponentUniqueName);
                            else
                                radioButton.IsChecked = info.defaultChecked;

                            if (radioButton.IsChecked == true)
                                hasSelection = true;
                        }
                        componentRadioStackPanel.Children.Add(radioButton);
                    }

                    ComponentListStackPanel.Children.Add(componentRadioGroupBox);
                }
                else if (node.Name.ToLower() == "prerequisite")
                {
                    Debug.WriteLine("prerequisite");
                    Prerequisites.Add(GetComponentFromXmlNode(node));
                }
                else if (node.Name.ToLower() == "compatfile")
                {
                    Debug.WriteLine("compatFile");
                    CompatFiles.Add(GetComponentFromXmlNode(node, true));
                }
                else if (node.Name.ToLower() == "remove")
                {
                    List<string> inner = node.InnerText.Split('?').ToList();

                    List<string> game = new List<string>();
                    if (node.Attributes["game"] != null)
                        game = node.Attributes["game"].Value.Split('?').ToList();
                    else
                        foreach (string s in inner)
                            game.Add(string.Empty);



                    Debug.WriteLine("remove");
                    Removals.Add(new RemovalInfo()
                    {
                        RemovalFileNames = inner,
                        RemovalGames = game
                    });
                }
            }

            int _activeComponentCount = GetComponentCount();
        }

        public string ComponentInfoToQuestionMarkSeparatedString(ComponentInfo info)
        {
            string returnValue = string.Empty;
            for (int i = 0; i < info.ComponentFileNames.Count; i++)
                returnValue = returnValue + info.ComponentFileNames[i] + "?" + info.ComponentGames[i] + "?";

            if (returnValue.EndsWith("?"))
                returnValue = returnValue.Substring(0, returnValue.LastIndexOf("?"));

            return returnValue;
        }

        public ComponentInfo QuestionMarkSeparatedStringToComponentInfo(string infoString)
        {
            ComponentInfo returnValue = new ComponentInfo();
            string[] strings = infoString.Split('?');
            for (int i = 0; i < strings.Count(); i += 2)
            {
                returnValue.ComponentFileNames.Add(strings[i]);
                returnValue.ComponentGames.Add(strings[i + 1]);
            }

            return returnValue;
        }

        private void Component_MouseEnter(object sender, MouseEventArgs e)
        {
            var sned = (sender as Control).Tag as ComponentInfo;
            NameTextBlock.Text = sned.ComponentDisplayName;
            DescriptionTextBlock.Text = sned.ComponentDescription;

            ComponentTopImage.Source = sned.ComponentPreviewImage;
            //ComponentTopImage.Height = sned.ComponentPreviewImage.PixelHeight / (ComponentTopImage.ActualWidth / sned.ComponentPreviewImage.PixelWidth);
            //ComponentBottomImage.Height = sned.ComponentPreviewImage.PixelHeight / (ComponentTopImage.ActualWidth / sned.ComponentPreviewImage.PixelWidth);
            if (sned.ComponentPreviewImagePlacement == ComponentInfo.ImagePlacement.InsteadOf)
            {
                ComponentTopImage.Visibility = Visibility.Visible;
                DescriptionTextBlock.Visibility = Visibility.Collapsed;
                ComponentBottomImage.Visibility = Visibility.Collapsed;
            }
            else if (sned.ComponentPreviewImagePlacement == ComponentInfo.ImagePlacement.Before)
            {
                ComponentTopImage.Visibility = Visibility.Visible;
                DescriptionTextBlock.Visibility = Visibility.Visible;
                ComponentBottomImage.Visibility = Visibility.Collapsed;
            }
            else if (sned.ComponentPreviewImagePlacement == ComponentInfo.ImagePlacement.After)
            {
                ComponentTopImage.Visibility = Visibility.Collapsed;
                DescriptionTextBlock.Visibility = Visibility.Visible;
                ComponentBottomImage.Visibility = Visibility.Visible;
            }
            else
            {
                ComponentTopImage.Visibility = Visibility.Collapsed;
                DescriptionTextBlock.Visibility = Visibility.Visible;
                ComponentBottomImage.Visibility = Visibility.Collapsed;
            }

            if ((ComponentInfoStackPanel.ActualHeight + ComponentInfoStackPanel.Margin.Top + ComponentInfoStackPanel.Margin.Bottom) > ComponentInfoScrollViewer.ScrollableHeight)
            {
                ComponentInfoScrollViewer.ScrollToVerticalOffset(0);
            }
        }


        private void Component_MouseLeave(object sender, MouseEventArgs e)
        {
            NameTextBlock.Text = ModDisplayName;
            DescriptionTextBlock.Text = ModDescription;
            ComponentTopImage.Visibility = Visibility.Collapsed;
            DescriptionTextBlock.Visibility = Visibility.Visible;
            ComponentBottomImage.Visibility = Visibility.Collapsed;
        }

        private ComponentInfo GetComponentFromXmlNode(XmlNode node)
        {
            return GetComponentFromXmlNode(node, false, false);
        }

        private ComponentInfo GetComponentFromXmlNode(XmlNode node, bool isCompatFile)
        {
            return GetComponentFromXmlNode(node, isCompatFile, false);
        }

        private ComponentInfo GetComponentFromXmlNode(XmlNode node, bool isCompatFile, bool hasUnique)
        {
            ComponentInfo info = new ComponentInfo()
            {
                ComponentFileNames = TrySplit(node.InnerText, '?')
                //ComponentFileName = node.InnerText
            };

            //if (Uri.IsWellFormedUriString(node.Attributes["name"].Value, UriKind.RelativeOrAbsolute))
            if (hasUnique)
            {
                if (node.Attributes["unique"] != null)
                    info.ComponentUniqueName = node.Attributes["unique"].Value;
                else
                    throw new Exception("Some of this mod's components lack display names.");

                if (!IsUniqueNameValid(info.ComponentUniqueName))
                    throw new Exception("Invalid unique name: " + info.ComponentUniqueName);
            }

            if (node.Attributes["displayName"] != null)
                info.ComponentDisplayName = node.Attributes["displayName"].Value;
            else
                info.ComponentDisplayName = info.ComponentUniqueName;

            //if (Uri.IsWellFormedUriString(node.Attributes["description"].Value, UriKind.RelativeOrAbsolute))
            if (node.Attributes["description"] != null)
                info.ComponentDescription = node.Attributes["description"].Value;

            //if (Uri.IsWellFormedUriString(node.Attributes["game"].Value, UriKind.RelativeOrAbsolute))
            if (node.Attributes["game"] != null)
            {
                info.ComponentGames = TrySplit(node.Attributes["game"].Value, '?'); //info.ComponentGame = node.Attributes["game"].Value;
            }
            else
            {
                foreach (string s in info.ComponentFileNames)
                    info.ComponentGames.Add(string.Empty);
            }

            if (isCompatFile)
            {
                //MessageBox.Show(node.Name.ToLower(), "node.Name.ToLower()");

                if (node.Attributes["compatTargetFileName"] != null)
                {
                    info.ComponentCompatTargetFileNames = TrySplit(node.Attributes["compatTargetFileName"].Value, '?');
                    //info.ComponentCompatTargetFileName = node.Attributes["compatTargetFileName"].Value;
                    //MessageBox.Show(node.Attributes["compatTargetFileName"].Value, "compatTargetFileName");
                }

                if (node.Attributes["compatTargetGame"] != null)
                {
                    info.ComponentCompatTargetGames = TrySplit(node.Attributes["compatTargetGame"].Value, '?');
                    //info.ComponentCompatTargetGame = node.Attributes["compatTargetGame"].Value;
                    //MessageBox.Show(node.Attributes["compatTargetGame"].Value, "compatTargetGame");
                }

                if ((node.Attributes["removeTargets"] != null) && bool.TryParse(node.Attributes["removeTargets"].Value, out bool removeTargets))
                {
                    info.removeTargets = removeTargets;
                }
            }

            //info.ComponentFileNames[0].Replace(".package", ".png").Replace(".dll", ".png")
            string previewImageName = Path.Combine(ModConfigPath, info.ComponentUniqueName + ".png");

            if (File.Exists(previewImageName))
            {
                //info.ComponentPreviewImage = new BitmapImage(new Uri(previewImageName, UriKind.RelativeOrAbsolute));
                BitmapImage previewImage = new BitmapImage();
                using (MemoryStream byteStream = new MemoryStream(File.ReadAllBytes(previewImageName)))
                {
                    previewImage.BeginInit();
                    previewImage.CacheOption = BitmapCacheOption.OnLoad;
                    previewImage.StreamSource = byteStream;
                    previewImage.EndInit();
                }
                info.ComponentPreviewImage = previewImage;

                try
                {
                    info.ComponentPreviewImagePlacement = (ComponentInfo.ImagePlacement)Enum.Parse(typeof(ComponentInfo.ImagePlacement), node.Attributes["imagePlacement"].Value, true);
                }
                catch
                {
                    info.ComponentPreviewImagePlacement = ComponentInfo.ImagePlacement.None;
                }
            }

            if (node.Attributes["defaultChecked"] != null)
            {
                info.defaultChecked = bool.Parse(node.Attributes["defaultChecked"].Value);
            }

            return info;
        }

        private List<string> TrySplit(string input, char splitMarker)
        {
            List<string> returnValue = new List<string>();
            if (input.Contains(splitMarker))
                returnValue = input.Split(splitMarker).ToList();
            else
                returnValue.Add(input);
            /*else
                info.ComponentFileNames = new List<string>()
                {
                    node.InnerText
                };*/
            return returnValue;
        }

        private void CollapseButtons()
        {
            StartInstallationButton.Visibility = Visibility.Collapsed;
            StartUninstallationButton.Visibility = Visibility.Collapsed;
        }

        private async void StartInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string mLibsFolder = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "mLibs");
                if (!Directory.Exists(mLibsFolder)) Directory.CreateDirectory(mLibsFolder);
                InstallationCompleteDescriptionTextBlock.Text = "The mod \"" + ModDisplayName + "\" has been installed.";
                _installerState = 1;
                CollapseButtons();
                
                var anim = new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    From = Height,
                    To = 112
                };
                anim.Completed += (sneder, args) =>
                {
                    Height = 112;
                    BeginAnimation(HeightProperty, null);
                };

                BeginAnimation(HeightProperty, anim);

                bool dontInstall = false;

                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    if ((Document.SelectSingleNode("/mod").Attributes["isExperimental"] != null) && bool.TryParse(Document.SelectSingleNode("/mod").Attributes["isExperimental"].Value, out bool isExperimental) && isExperimental)
                    {
                        var result = MessageBox.Show("This mod \"" + ModDisplayName + "\" is still in an experimental state, and its use may have unexpected, potentially undesirable consequences. Are you sure you wish to install it?", "Experimental mod", MessageBoxButton.YesNo);
                        if (result != MessageBoxResult.Yes)
                        {
                            dontInstall = true;
                        }
                    }

                    if ((Document.SelectSingleNode("/mod").Attributes["requiresGalaxyReset"] != null) && bool.TryParse(Document.SelectSingleNode("/mod").Attributes["requiresGalaxyReset"].Value, out bool reqGalaxyReset) && reqGalaxyReset)
                    {
                        var result = MessageBox.Show("The mod \"" + ModDisplayName + "\" can be installed, but will require a Galaxy reset to take effect. Performing a Galaxy reset will erase all of your save game planets. The necessary Galaxy Reset will NOT be performed automatically, you'll have to do it by hand. Are you sure you wish to install this mod?", "Mod requires a Galaxy Reset", MessageBoxButton.YesNo);
                        if (result != MessageBoxResult.Yes)
                        {
                            dontInstall = true;
                        }
                    }

                    if ((Document.SelectSingleNode("/mod").Attributes["causesSaveDataDependency"] != null) && bool.TryParse(Document.SelectSingleNode("/mod").Attributes["requiresGalaxyReset"].Value, out bool causesSaveDataDependency) && causesSaveDataDependency)
                    {
                        var result = MessageBox.Show("The mod \"" + ModDisplayName + "\" will cause save data dependency if installed. This means that if you ever uninstall it, your save planets may become corrupted or otherwise inaccessible, or be adversely affected in some other way. Are you sure you wish to install this mod?", "Mod causes Save Data Dependency", MessageBoxButton.YesNo);
                        if (result != MessageBoxResult.Yes)
                        {
                            dontInstall = true;
                        }
                    }
                }));

                if (dontInstall)
                {
                    XmlInstallerCancellation.Cancellation[ModName] = true;
                    _installerState = 2;
                    _installerMode = 1;
                    _result = ResultType.ModNotInstalled;
                    Close();
                }


                List<string> enabledComponents = new List<string>();
                InstallProgressBar.Maximum = GetComponentCount(); //enabledComponents.Count + Prerequisites.Count;

                // only respect <remove> during installation
                if (!_isConfigurator)
                {
                    foreach (RemovalInfo p in Removals)
                        p.Remove();
                }

                foreach (ComponentInfo p in Prerequisites)
                {
                    await Task.Run(() => HandleModComponent(p));
                    if (InstallProgressBar.Value < InstallProgressBar.Maximum)
                        InstallProgressBar.Value += 1;
                }

                List<string> storedSettings = new List<string>();
                foreach (Control c in ComponentListStackPanel.Children)
                {
                    if (c is CheckBox)
                    {
                        var info = c.Tag as ComponentInfo;
                        storedSettings.Add(CreateSettingStorageString(info.ComponentUniqueName, (c as CheckBox).IsChecked.Value.ToString()));

                        if ((c as CheckBox).IsChecked.Value)
                        {
                            await Task.Run(() => HandleModComponent(info));
                            enabledComponents.Add(ComponentInfoToQuestionMarkSeparatedString(info)/*info.ComponentFileName + "?" + info.ComponentGame*/);

                            if (InstallProgressBar.Value < InstallProgressBar.Maximum)
                                InstallProgressBar.Value += 1;
                        }
                        else if (_isConfigurator)
                        {
                            await Task.Run(() => HandleModComponent(info, false));
                        }
                    }
                    else if (c is GroupBox)
                    {
                        string selected = string.Empty;
                        foreach (RadioButton b in ((c as GroupBox).Content as StackPanel).Children)
                        {
                            var info = b.Tag as ComponentInfo;
                            if ((b as RadioButton).IsChecked.Value)
                            {
                                await Task.Run(() => HandleModComponent(info));
                                selected = info.ComponentUniqueName;
                                enabledComponents.Add(ComponentInfoToQuestionMarkSeparatedString(info)/*info.ComponentFileName + "?" + info.ComponentGame*/);

                                if (InstallProgressBar.Value < InstallProgressBar.Maximum)
                                    InstallProgressBar.Value += 1;
                            }
                            else if (_isConfigurator)
                            {
                                await Task.Run(() => HandleModComponent(info, false));
                            }
                        }
                        storedSettings.Add(CreateSettingStorageString((c as GroupBox).Tag.ToString(), selected));
                    }
                }

                foreach (ComponentInfo p in CompatFiles)
                {
                    await Task.Run(() => HandleCompatFile(p));
                    if (InstallProgressBar.Value < InstallProgressBar.Maximum)
                        InstallProgressBar.Value += 1;
                }

                // only write settings if they exist
                if (storedSettings.Count > 0)
                {
                    File.WriteAllLines(ModSettingsStoragePath, storedSettings.ToArray());
                }

                if (_installerMode == 0)
                {
                    ModConfiguration mod = new ModConfiguration(ModName, ModUnique)
                    {
                        DisplayName = ModDisplayName,
                        Version = ModVersion,
                        Dependencies = ModDependencies.ToArray(),
                        DependenciesVersions = ModDependenciesVersions.ToArray(),
                        ConfiguratorPath = Path.Combine(ModConfigPath, "ModInfo.xml")
                    };

                    mod.InstalledFiles.Add(new InstalledFile(ModName));

                    for (int i = 0; i < EasyInstaller.ModList.ModConfigurations.Count; i++)
                    {
                        ModConfiguration selMod = EasyInstaller.ModList.ModConfigurations.ElementAt(i);

                        if ((selMod.Name == ModName) || (selMod.Unique == ModUnique))
                        {
                            EasyInstaller.ModList.ModConfigurations.Remove(selMod);
                            if (i > 0)
                                i--;
                        }
                    }

                    EasyInstaller.ModList.ModConfigurations.Add(mod);
                }
                else if (_installerMode == 1)
                {
                    ModConfiguration mod = new ModConfiguration(ModName, ModUnique)
                    {
                        DisplayName = ModDisplayName,
                        Version = ModVersion,
                        Dependencies = ModDependencies.ToArray(),
                        DependenciesVersions = ModDependenciesVersions.ToArray(),
                    };

                    foreach (ComponentInfo c in Prerequisites)
                    {
                        for (int i = 0; i < c.ComponentFileNames.Count; i++)
                        {
                            InstalledFile file = null;
                            if (c.ComponentGames[i] == GameSporeString)
                                file = new InstalledFile(c.ComponentFileNames[i], SporePath.Game.Spore);
                            else if (c.ComponentGames[i] == GameGaString)
                                file = new InstalledFile(c.ComponentFileNames[i]);
                            else
                                file = new InstalledFile(c.ComponentFileNames[i], SporePath.Game.None);

                            mod.InstalledFiles.Add(file);
                        }
                    }
                    foreach (ComponentInfo c in InstalledCompatFiles)
                    {
                        for (int i = 0; i < c.ComponentFileNames.Count; i++)
                        {
                            InstalledFile file = null;

                            if (c.ComponentGames[i] == GameSporeString)
                                file = new InstalledFile(c.ComponentFileNames[i], SporePath.Game.Spore);
                            else if (c.ComponentGames[i] == GameGaString)
                                file = new InstalledFile(c.ComponentFileNames[i]);
                            else
                                file = new InstalledFile(c.ComponentFileNames[i], SporePath.Game.None);

                            mod.InstalledFiles.Add(file);
                        }
                    }

                    for (int i = 0; i < EasyInstaller.ModList.ModConfigurations.Count; i++)
                    {
                        ModConfiguration selMod = EasyInstaller.ModList.ModConfigurations.ElementAt(i);

                        if ((selMod.Name == ModName) || (selMod.Unique == ModUnique))
                        {
                            EasyInstaller.ModList.ModConfigurations.Remove(selMod);
                            if (i > 0)
                                i--;
                        }
                    }

                    EasyInstaller.ModList.ModConfigurations.Add(mod);
                }
                if (_installerMode == 1)
                {
                    _installerState = 2;
                    _result = ResultType.Success;
                    Close();
                }
                else
                {
                    CyclePage(0, 1);
                    _result = ResultType.Success;
                    _installerState = 2;
                }
            }
            catch (Exception ex)
            {
                _result = ResultType.ModNotInstalled;
                ErrorInfoTextBlock.Text = ex.ToString();
                CyclePage(0, 3);
            }
        }

        bool IsUniqueNameValid(string value)
        {
            bool returnValue = true;

            foreach (char c in Path.GetInvalidPathChars())
            {
                if (value.Contains(c))
                {
                    returnValue = false;
                    break;
                }
            }

            return returnValue;
        }

        string GetSettingValueFromStorageString(string setting)
        {
            if (File.Exists(ModSettingsStoragePath))
            {
                string[] storedSettings = File.ReadAllLines(ModSettingsStoragePath);

                string returnValue = null;
                foreach (string s in storedSettings)
                {
                    string settingName = s.Substring(0, s.IndexOf(">")).Replace("<", "");
                    if (settingName == setting)
                    {
                        returnValue = s.Substring(s.IndexOf(">") + 1);
                        break;
                    }
                }

                return returnValue;
            }
            else
                return null;
        }

        string CreateSettingStorageString(string componentName, string value)
        {
            return "<" + componentName + ">" + value;
        }

        private async void StartUninstallationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InstallationCompleteHeaderTextBlock.Text = "Uninstallation complete!";
                InstallationCompleteDescriptionTextBlock.Text = "Your mod has been uninstalled.";

                _installerState = 1;
                CollapseButtons();

                var anim = new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    From = Height,
                    To = 112
                };
                anim.Completed += (sneder, args) =>
                {
                    Height = 112;
                    BeginAnimation(HeightProperty, null);
                };

                BeginAnimation(HeightProperty, anim);

                InstallProgressBar.Maximum = _activeComponentCount;

                foreach (ComponentInfo p in Prerequisites)
                {
                    await Task.Run(() => HandleModComponent(p, false));
                }

                foreach (Control c in ComponentListStackPanel.Children)
                {
                    if (c is CheckBox)
                    {
                        var info = c.Tag as ComponentInfo;
                        await Task.Run(() => HandleModComponent(info, false));
                    }
                    else if (c is GroupBox)
                    {
                        foreach (RadioButton b in ((c as GroupBox).Content as StackPanel).Children)
                        {
                            var info = b.Tag as ComponentInfo;
                            await Task.Run(() => HandleModComponent(info, false));
                        }
                    }
                }

                foreach (ComponentInfo p in CompatFiles)
                {
                    await Task.Run(() =>
                    {
                        for (int i = 0; i < p.ComponentFileNames.Count; i++)
                        {
                            string targetPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), p.ComponentFileNames[i]);

                            if (p.ComponentGames[i] == GameGaString)
                                targetPath = Path.Combine(GaDataPath, p.ComponentFileNames[i]);
                            else if (p.ComponentGames[i] == GameSporeString)
                                targetPath = Path.Combine(SporeDataPath, p.ComponentFileNames[i]);

                            if (File.Exists(targetPath))
                                File.Delete(targetPath);
                        }
                    });
                }

                for (int i = 0; i < EasyInstaller.ModList.ModConfigurations.Count; i++)
                {
                    ModConfiguration mod = EasyInstaller.ModList.ModConfigurations.ElementAt(i);

                    if ((mod.Name == ModName) || (mod.Unique == ModUnique))
                    {
                        EasyInstaller.ModList.ModConfigurations.Remove(mod);
                        if (i > 0)
                            i--;
                    }
                }
                EasyInstaller.ModList.Save();

                if (File.Exists(ModSettingsStoragePath))
                    File.Delete(ModSettingsStoragePath);

                await Task.Run(() => DeleteFolder(ModConfigPath));
                CyclePage(0, 1);
                _installerState = 2;

                if (_isUninstallingOnly)
                {
                    _result = ResultType.Success;
                    Close();
                }
            }
            catch (Exception ex)
            {
                // dummy error so that the uninstaller
                // knows this mod failed to uninstall
                if (_isUninstallingOnly)
                {
                    _result = ResultType.UnsupportedFile;
                }
                ErrorInfoTextBlock.Text = ex.ToString();
                CyclePage(0, 3);
            }
        }

        private int GetComponentCount()
        {
            int count = Prerequisites.Count + CompatFiles.Count;

            if (_isConfigurator)
                count += Removals.Count;

            foreach (Control c in ComponentListStackPanel.Children)
            {
                if (c is CheckBox)
                {
                    if ((c as CheckBox).IsChecked.Value)
                        count++;
                }
                else if (c is GroupBox)
                {
                    foreach (RadioButton b in ((c as GroupBox).Content as StackPanel).Children)
                    {
                        if ((b as RadioButton).IsChecked.Value)
                            count++;
                    }
                }
            }
            return count;
        }

        private void HandleCompatFile(ComponentInfo p)
        {
            List<string> targets = new List<string>();
            bool allComponentsExist = true;
            for (int i = 0; i < p.ComponentCompatTargetFileNames.Count; i++)
            {
                string[] targetPaths = new string[]
                    {
                    Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), p.ComponentCompatTargetFileNames[i]),
                    Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "mLibs", p.ComponentCompatTargetFileNames[i])
                    };

                if (p.ComponentCompatTargetGames[i] == GameGaString)
                    targetPaths = new string[] { Path.Combine(GaDataPath, p.ComponentCompatTargetFileNames[i]) };
                else if (p.ComponentCompatTargetGames[i] == GameSporeString)
                    targetPaths = new string[] { Path.Combine(SporeDataPath, p.ComponentCompatTargetFileNames[i]) };

                if (targetPaths.Length == 1)
                {
                    if (!File.Exists(targetPaths[0]))
                    {
                        allComponentsExist = false;
                        break;
                    }
                }
                else if (targetPaths.Length == 2)
                {
                    if (!(File.Exists(targetPaths[0]) || File.Exists(targetPaths[1])))
                    {
                        allComponentsExist = false;
                        break;
                    }
                }

                targets.AddRange(targetPaths);
            }

            if (allComponentsExist)
            {
                if (p.removeTargets)
                {
                    foreach (string s in targets)
                        File.Delete(s);
                }

                HandleModComponent(p);
                InstalledCompatFiles.Add(p);
            }
        }

        private void HandleModComponent(ComponentInfo p, bool copy = true)
        {
            for (int i = 0; i < p.ComponentFileNames.Count; i++)
            {
                HandleModFile(p, i, copy);
            }
        }

        private void HandleModFile(ComponentInfo p, int index, bool copy)
        {
            bool isDll = false;
            string targetPath;
            string targetFolderPath = string.Empty;
            if (p.ComponentGames[index] == GameGaString)
                targetFolderPath = GaDataPath;
            else if (p.ComponentGames[index] == GameSporeString)
                targetFolderPath = SporeDataPath;
            else
            {
                isDll = true;
                if (_dontUseLegacyPackagePlacement)
                {
                    string dllFolderPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "mLibs");
                    if (!Directory.Exists(dllFolderPath))
                        Directory.CreateDirectory(dllFolderPath);
                    string outputPath = Path.Combine(dllFolderPath, p.ComponentFileNames[index]);

                    if (File.Exists(outputPath))
                        File.Delete(outputPath);

                    if (copy)
                    {
                        string inPath = Path.Combine(ModConfigPath, p.ComponentFileNames[index]);
                        if (File.Exists(inPath))
                            File.Copy(inPath, outputPath);
                    }
                }
                else
                {

                    if (p.ComponentFileNames[index].EndsWith("-disk.dll") || p.ComponentFileNames[index].EndsWith("-steam.dll") || p.ComponentFileNames[index].EndsWith("-steam_patched.dll"))
                    {
                        string outputPath = Path.Combine(
                            Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString()
                            , p.ComponentFileNames[index]);


                        if (File.Exists(outputPath))
                        {
                            File.Delete(outputPath);
                        }

                        if (copy)
                        {
                            string inPath = Path.Combine(ModConfigPath, p.ComponentFileNames[index]);
                            if (File.Exists(inPath))
                                File.Copy(inPath, outputPath);
                        }
                    }
                    else
                    {
                        string outputPath = Path.Combine(
                            Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString()
                            , p.ComponentFileNames[index].Replace(".dll", "-disk.dll"));

                        if (File.Exists(outputPath))
                        {
                            File.Delete(outputPath);
                        }

                        if (copy)
                        {
                            string inPath = Path.Combine(ModConfigPath, p.ComponentFileNames[index].Replace(".dll", "-disk.dll"));
                            if (File.Exists(inPath))
                                File.Copy(inPath, outputPath);
                        }

                        outputPath = Path.Combine(
                            Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString()
                            , p.ComponentFileNames[index].Replace(".dll", "-steam.dll"));
                        if (File.Exists(outputPath))
                        {
                            File.Delete(outputPath);
                        }

                        if (copy)
                        {
                            string inPath = Path.Combine(ModConfigPath, p.ComponentFileNames[index].Replace(".dll", "-steam.dll"));
                            if (File.Exists(inPath))
                                File.Copy(inPath, outputPath);
                        }

                        outputPath = Path.Combine(
                            Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString()
                            , p.ComponentFileNames[index].Replace(".dll", "-steam_patched.dll"));
                        if (File.Exists(outputPath))
                        {
                            File.Delete(outputPath);
                        }

                        if (copy)
                        {
                            string inPath = Path.Combine(ModConfigPath, p.ComponentFileNames[index].Replace(".dll", "-steam_patched.dll"));
                            if (File.Exists(inPath))
                                File.Copy(inPath, outputPath);
                        }
                    }
                }
            }

            if (!isDll)
            {
                targetPath = Path.Combine(targetFolderPath, p.ComponentFileNames[index]);

                if (File.Exists(targetPath))
                    File.Delete(targetPath);

                if (copy)
                {
                    string inPath = Path.Combine(ModConfigPath, p.ComponentFileNames[index]);
                    if (File.Exists(inPath))
                        File.Copy(inPath, targetPath);
                }
            }
        }

        private void DeleteFolder(string path)
        {
            foreach (string s in Directory.EnumerateFiles(path))
                File.Delete(s);

            foreach (string s in Directory.EnumerateDirectories(path))
                DeleteFolder(s);

            Directory.Delete(path);
        }

        public void CyclePage(int currentPage, int targetPage)
        {
            WindowBodyRootGrid.Children[targetPage].Visibility = Visibility.Visible;

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
            FadeOutAnimation.Completed += (sneder, args) => WindowBodyRootGrid.Children[currentPage].Visibility = Visibility.Collapsed;

            WindowBodyRootGrid.Children[currentPage].RenderTransform = new TranslateTransform();
            WindowBodyRootGrid.Children[targetPage].RenderTransform = new TranslateTransform();

            WindowBodyRootGrid.Children[currentPage].BeginAnimation(Panel.OpacityProperty, FadeOutAnimation);
            (WindowBodyRootGrid.Children[currentPage].RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.XProperty, GlideOutAnimation);
            WindowBodyRootGrid.Children[targetPage].BeginAnimation(Panel.OpacityProperty, FadeInAnimation);
            (WindowBodyRootGrid.Children[targetPage].RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.XProperty, GlideInAnimation);

            if ((targetPage != 0) && (targetPage != 3))
            {
                double initialHeight = Height;
                double targetHeight = BorderThickness.Top + BorderThickness.Bottom + ((WindowBodyRootGrid.Children[targetPage]) as FrameworkElement).ActualHeight + 115;
                DoubleAnimation WindowHeightAnimation = new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    From = initialHeight,
                    To = targetHeight
                };
                WindowHeightAnimation.Completed += (sneder, args) =>
                {
                    Height = targetHeight;
                    BeginAnimation(HeightProperty, null);
                };
                BeginAnimation(HeightProperty, WindowHeightAnimation);
            }
            else if (targetPage == 3)
            {
                ResizeMode = ResizeMode.CanResize;
                _installerState = 3;
            }
        }

        /*public new bool? ShowDialog()
        {
            base.ShowDialog();
            return cancelled;
        }*/
        /*private void SetSplashIsActive()
        {
            if (SplashBorder.IsVisible)
            {
                if (IsActive)
                {
                    ActiveSplashBackground.Visibility = Visibility.Visible;
                    InactiveSplashBackground.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ActiveSplashBackground.Visibility = Visibility.Collapsed;
                    InactiveSplashBackground.Visibility = Visibility.Visible;
                }
            }
        }*/

        private void XmlInstallerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_installerState == 0)
            {
                if (!_isConfigurator && (!_isModMatched))
                {
                    if (Directory.Exists(ModConfigPath))
                        DeleteFolder(ModConfigPath);
                    
                }
                XmlInstallerCancellation.Cancellation[ModName] = true;
            }
            else if (_installerState == 1) e.Cancel = true;
            else if (_installerState == 2 && _installerMode == 1)
            {
                if (Directory.Exists(ModConfigPath))
                    DeleteFolder(ModConfigPath);

            }
        }

        private void XmlInstallerWindow_Closed(object sender, EventArgs e)
        {
            if (installerWindows.Contains(this))
                installerWindows.Remove(this);
            if (_isConfigurator)
                Environment.Exit((int)_result);
        }
                        
        private void SplashBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class ComponentInfo
    {
        public enum ImagePlacement
        {
            Before,
            After,
            InsteadOf,
            None
        }

        public string ComponentUniqueName = string.Empty;
        public string ComponentDisplayName = string.Empty;
        public string ComponentDescription = string.Empty;
        public List<string> ComponentFileNames = new List<string>();
        public List<string> ComponentGames = new List<string>();
        public List<string> ComponentCompatTargetFileNames = new List<string>();
        public List<string> ComponentCompatTargetGames = new List<string>();
        public BitmapImage ComponentPreviewImage = new BitmapImage();
        public ImagePlacement ComponentPreviewImagePlacement = ImagePlacement.None;
        public bool defaultChecked = false;
        public bool removeTargets = false;
    }

    public class RemovalInfo
    {
        public List<string> RemovalFileNames = new List<string>();
        public List<string> RemovalGames = new List<string>();

        public void Remove()
        {
            for (int i = 0; i < RemovalFileNames.Count; i++)
            {
                DirectoryInfo[] infos;

                if (RemovalGames[i] == XmlInstallerWindow.GameGaString)
                    infos = new DirectoryInfo[] { new DirectoryInfo(XmlInstallerWindow.GaDataPath) };
                else if (RemovalGames[i] == XmlInstallerWindow.GameSporeString)
                    infos = new DirectoryInfo[] { new DirectoryInfo(XmlInstallerWindow.SporeDataPath) };
                else
                    infos = new DirectoryInfo[] {
                        new DirectoryInfo(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString()),
                        new DirectoryInfo(Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "mLibs"))
                    };
                foreach (DirectoryInfo info in infos) {
                List<FileInfo> files = info.EnumerateFiles(RemovalFileNames[i]).ToList();

                foreach (FileInfo f in files)
                {
                    if (File.Exists(f.FullName))
                        File.Delete(f.FullName);
                } }
            }
        }
    }
}