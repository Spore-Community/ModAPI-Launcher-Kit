using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Xml;

namespace ModAPI.Common
{

    public class InstalledFile
    {
        public static readonly string ElementName = "file";

        public string Name;
        // public SporePath.Game PathType = SporePath.Game.None;  // set to None if it's a dll
        public string PathType;

        public InstalledFile(string name, SporePath.Game pathType = SporePath.Game.GalacticAdventures)
        {
            this.Name = name;
            this.PathType = pathType.ToString();
        }

        public InstalledFile(string name, string pathType)
        {
            this.Name = name;
            this.PathType = pathType;
        }

        public void Save(XmlDocument document, XmlElement parent)
        {
            var element = document.CreateElement(InstalledFile.ElementName);

            element.InnerText = Name;

            if (PathType != null && PathType != SporePath.Game.None.ToString())
            {
                var attribute = document.CreateAttribute("game");
                attribute.Value = PathType.ToString();
                element.Attributes.Append(attribute);
            }


            parent.AppendChild(element);
        }
    }


    public class ModConfiguration : IEquatable<ModConfiguration>
    {
        public static readonly string ElementName = "mod";

        public string Name;
        public string Unique;
        public Version Version;
        public string DisplayName;
        public string ConfiguratorPath;
        public string[] Dependencies;
        public Version[] DependenciesVersions;
        public List<InstalledFile> InstalledFiles = new List<InstalledFile>();

        public ModConfiguration(string name, string unique)
        {
            Name = name;
            Unique = unique;
        }

        public ModConfiguration(string name)
        {
            Name = name;
            Unique = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public void AddFile(string name, SporePath.Game pathType = SporePath.Game.None)
        {
            this.InstalledFiles.Add(new InstalledFile(name, pathType));
        }

        public void RemoveFile(InstalledFile file)
        {
            this.InstalledFiles.Remove(file);
        }

        public void Save(XmlDocument document, XmlElement parent)
        {
            var element = document.CreateElement(ModConfiguration.ElementName);

            var attribute = document.CreateAttribute("name");
            attribute.Value = this.Name;
            element.Attributes.Append(attribute);

            /*if (Unique != null)
            {*/
            var uniqueAttribute = document.CreateAttribute("unique");
            uniqueAttribute.Value = Unique;
            element.Attributes.Append(uniqueAttribute);
            //}

            if (Version != null)
            {
                var versionAttribute = document.CreateAttribute("version");
                versionAttribute.Value = Version.ToString();
                element.Attributes.Append(versionAttribute);
            }

            if (Dependencies != null && Dependencies.Length > 0)
            {
                var dependenciesAttribute = document.CreateAttribute("dependencies");
                dependenciesAttribute.Value = String.Join("?", Dependencies);
                element.Attributes.Append(dependenciesAttribute);
            }

            if (DependenciesVersions != null && DependenciesVersions.Length > 0)
            {
                var dependenciesVersionsAttribute = document.CreateAttribute("dependenciesVersions");
                dependenciesVersionsAttribute.Value = String.Join("?", DependenciesVersions.Select(x => x.ToString()));
                element.Attributes.Append(dependenciesVersionsAttribute);
            }

            if (this.ConfiguratorPath != null)
            {
                attribute = document.CreateAttribute("configurator");
                attribute.Value = this.ConfiguratorPath;
                element.Attributes.Append(attribute);
            }

            if ((DisplayName != null) && (DisplayName != Name))
            {
                var displayAttribute = document.CreateAttribute("displayName");
                displayAttribute.Value = this.DisplayName;
                element.Attributes.Append(displayAttribute);
            }

            foreach (InstalledFile file in InstalledFiles)
            {
                file.Save(document, element);
            }

            parent.AppendChild(element);
        }

        public void Load(XmlNode node)
        {
            var nameAttribute = node.Attributes.GetNamedItem("name");
            if (nameAttribute != null)
                this.Name = nameAttribute.Value;

            var uniqueAttribute = node.Attributes.GetNamedItem("unique");
            if (uniqueAttribute != null)
                Unique = uniqueAttribute.Value;
            else
                Unique = nameAttribute.Value;

            var versionAttribute = node.Attributes.GetNamedItem("version");
            if (versionAttribute != null &&
                Version.TryParse(versionAttribute.Value, out Version parsedVersion))
            {
                Version = parsedVersion;
            }
            else
            {
                Version = null;
            }

            var dependenciesAttribute = node.Attributes.GetNamedItem("dependencies");
            if (dependenciesAttribute != null)
            {
                Dependencies = dependenciesAttribute.Value.Split('?');
            }

            var dependenciesVersionsAttribute = node.Attributes.GetNamedItem("dependenciesVersions");
            if (dependenciesVersionsAttribute != null)
            {
                List<Version> versions = new List<Version>();
                foreach (string version in dependenciesVersionsAttribute.Value.Split('?'))
                {
                    if (Version.TryParse(version, out Version parsedDependencyVersion))
                    {
                        versions.Add(parsedDependencyVersion);
                    }
                    else
                    {
                        versions.Clear();
                        break;
                    }
                }

                if (versions.Count() != 0)
                {
                    DependenciesVersions = versions.ToArray();
                }
            }

            var displayNameAttribute = node.Attributes.GetNamedItem("displayName");
            if (displayNameAttribute != null)
                this.DisplayName = displayNameAttribute.Value;
            else if (nameAttribute != null)
                this.DisplayName = nameAttribute.Value;

            var configuratorAttribute = node.Attributes.GetNamedItem("configurator");
            if (configuratorAttribute != null)
            {
                this.ConfiguratorPath = configuratorAttribute.Value;
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == InstalledFile.ElementName)
                {
                    // var game = SporePath.Game.None;
                    string game = null;

                    var attribute = child.Attributes.GetNamedItem("game");
                    if (attribute != null)
                    {
                        // game = (SporePath.Game) Enum.Parse(typeof(SporePath.Game), attribute.Value, true);
                        game = attribute.Value;
                    }
                    else
                    {
                        game = "None";  // that is, DLLs (when the value is None no attribute is written, so we fix it here)
                    }

                    InstalledFiles.Add( new InstalledFile(child.InnerText, game) );
                }
            }
        }

        public override int GetHashCode()
        {
            return Name == null ? 0 : Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ModConfiguration && Equals((ModConfiguration) obj);
        }

        public bool Equals(ModConfiguration obj)
        {
            return obj == null ? false : obj.Name == Name;
        }
    }

    public class InstalledMods
    {
        public static readonly string FileName = "InstalledMods.config";

        public static readonly string ElementName = "InstalledMods";

        public HashSet<ModConfiguration> ModConfigurations = new HashSet<ModConfiguration>();

        public ModConfiguration AddMod(string name)
        {
            var mod = new ModConfiguration(name);
            // first remove anything that has the same name, then add the new one
            ModConfigurations.Remove(mod);
            ModConfigurations.Add(mod);
            return mod;
        }

        public void RemoveMod(ModConfiguration mod)
        {
            this.ModConfigurations.Remove(mod);

        }

        public void Save()
        {
            string path = GetDefaultPath();
            var document = new XmlDocument();

            var mainNode = document.CreateElement(InstalledMods.ElementName);
            document.AppendChild(mainNode);

            foreach (ModConfiguration mod in ModConfigurations)
            {
                mod.Save(document, mainNode);
            }

            document.Save(path);
        }

        public void Load(XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == ModConfiguration.ElementName)
                {
                    var mod = new ModConfiguration(null);
                    mod.Load(child);

                    ModConfigurations.Add(mod);
                }

            }
        }

        public bool Load()
        {
            ModConfigurations.Clear();

            string path = GetDefaultPath();
            if (File.Exists(path))
            {
                string xmlIn = File.ReadAllText(path);
                if (!String.IsNullOrEmpty(xmlIn))
                {
                    var document = new XmlDocument();
                    document.LoadXml(xmlIn);

                    foreach (XmlNode node in document.ChildNodes)
                    {
                        if (node.Name == InstalledMods.ElementName)
                        {
                            Load(node);
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static string GetDefaultPath()
        {
            var programPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            return Path.Combine(programPath, FileName);
        }
    }
}
