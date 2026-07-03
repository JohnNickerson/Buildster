using System.Text;
using System.Xml;
using System.IO.Abstractions;
using LibGit2Sharp;
using AssimilationSoftware.Buildster.Core.Interfaces;

namespace AssimilationSoftware.Buildster.Core.Utils
{
    public class VersionInfo
    {
        private IFileSystem _fileSystem;
        private string _path;

        public VersionInfo(string path, IFileSystem fileSystem = null)
        {
            _path = path;
            _fileSystem = fileSystem ?? new FileSystem();
        }

        public void UpdateCopyright(string company, int year, IStatusWriter statusWriter)
        {
            UpdateCopyright(_path, company, year, statusWriter, _fileSystem);
        }

        public static void Update(string path, VersionPart part, IStatusWriter statusWriter)
        {
            var currentVersion = Get(path, statusWriter).FirstOrDefault();
            switch (part)
            {
                case VersionPart.Patch:
                    currentVersion.Patch++;
                    break;
                case VersionPart.Revision:
                    currentVersion.Revision++;
                    currentVersion.Patch = 0;
                    break;
                case VersionPart.Minor:
                    currentVersion.Minor++;
                    currentVersion.Revision = 0;
                    currentVersion.Patch = 0;
                    break;
                case VersionPart.Major:
                    currentVersion.Major++;
                    currentVersion.Minor = 0;
                    currentVersion.Revision = 0;
                    currentVersion.Patch = 0;
                    break;
            }
            Update(path, currentVersion, statusWriter);
        }

        public static void Update(string path, VersionNumber newVersion, IStatusWriter statusWriter)
        {
            var newAssemblyVersion = new AssemblyAttribute("AssemblyVersion", newVersion.ToString());
            var newFileVersion = new AssemblyAttribute("AssemblyFileVersion", newVersion.ToString());
            foreach (var file in Directory.EnumerateFiles(ExtensionMethods.PathExpandCombine(path), "AssemblyInfo.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file, Encoding.UTF8).ToList();
                for (int x = 0; x < lines.Count; x++)
                {
                    if (AssemblyAttribute.TryParse(lines[x], out var attrib))
                    {
                        if (attrib.Name == "AssemblyVersion")
                        {
                            if (lines[x] != newAssemblyVersion.ToString())
                            {
                                // Replace with the new version number.
                                statusWriter?.Write($"Replacing {lines[x]} in {file} with {newAssemblyVersion}");
                                lines[x] = newAssemblyVersion.ToString();
                            }
                        }
                        else if (attrib.Name == "AssemblyFileVersion")
                        {
                            if (lines[x] != newFileVersion.ToString())
                            {
                                // Replace with the new version number.
                                statusWriter?.Write($"Replacing {lines[x]} in {file} with {newFileVersion}");
                                lines[x] = newFileVersion.ToString();
                            }
                        }
                    }
                }
                File.WriteAllLines(file, lines, Encoding.UTF8);
            }
            var propertyGroupPath = "Project/PropertyGroup";
            var xPaths = new string[] { "Project/PropertyGroup/Version", "Project/PropertyGroup/FileVersion", "Project/PropertyGroup/InformationalVersion", "Project/PropertyGroup/PackageVersion" };
            foreach (var file in Directory.EnumerateFiles(ExtensionMethods.PathExpandCombine(path), "*.csproj", SearchOption.AllDirectories))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);
                foreach (var xPath in xPaths)
                {
                    var versionNode = xmlDoc.SelectSingleNode(xPath);
                    if (versionNode != null)
                    {
                        if (versionNode.InnerText != newVersion.ToString())
                        {
                            statusWriter?.Write($"Replacing {versionNode.InnerText} in {file} with {newVersion}");
                            versionNode.InnerText = newVersion.ToString();
                        }
                    }
                    else
                    {
                        var propertyGroupNode = xmlDoc.SelectSingleNode(propertyGroupPath);
                        if (propertyGroupNode != null)
                        {
                            // Add the node.
                            versionNode = xmlDoc.CreateElement(xPath.Split('/').Last());
                            versionNode.InnerText = newVersion.ToString();
                            propertyGroupNode.AppendChild(versionNode);
                        }
                    }
                }
                xmlDoc.Save(file);
            }
        }

        public static void Tag(string path, VersionNumber versionNumber, IStatusWriter statusWriter)
        {
            string tagString = $"Build.{versionNumber}";
            try
            {
                var gitRepo = new Repository(ExtensionMethods.PathExpandCombine(path));
                if (gitRepo.Tags[tagString] != null)
                {
                    statusWriter?.Write($"Tag {tagString} already exists in repository at {path}");
                    return;
                }
                gitRepo.ApplyTag(tagString);
                if (gitRepo.Network.Remotes.FirstOrDefault(r => r.Name == "origin") == null)
                {
                    statusWriter?.Write($"No remote named 'origin' found in repository at {path}. Skipping push of tag {tagString}.");
                    return;
                }
                gitRepo.Network.Push(gitRepo.Network.Remotes.FirstOrDefault(r => r.Name == "origin"), $"refs/tags/{tagString}");
            }
            catch (Exception ex)
            {
                statusWriter?.Write($"Error tagging repository at {path} with tag {tagString}: {ex.Message}");
            }
        }

        public static void UpdateCopyright(string path, string company, int year, IStatusWriter statusWriter, IFileSystem fileSystem = null)
        {
            fileSystem ??= new FileSystem();
            var newCopyright = new AssemblyAttribute("AssemblyCopyright", $"Copyright © {company} {year}");
            foreach (var file in fileSystem.Directory.EnumerateFiles(ExtensionMethods.PathExpandCombine(path), "AssemblyInfo.cs", SearchOption.AllDirectories))
            {
                var lines = fileSystem.File.ReadAllLines(file, Encoding.UTF8).ToList();
                for (int x = 0; x < lines.Count; x++)
                {
                    if (AssemblyAttribute.TryParseExact(lines[x], "AssemblyCopyright", out _) && lines[x] != newCopyright.ToString())
                    {
                        statusWriter?.Write($"Replacing {lines[x]} in {file} with {newCopyright}");
                        lines[x] = newCopyright.ToString();
                    }
                }
                fileSystem.File.WriteAllLines(file, lines, Encoding.UTF8);
            }
            foreach (var file in fileSystem.Directory.EnumerateFiles(ExtensionMethods.PathExpandCombine(path), "*.csproj", SearchOption.AllDirectories))
            {
                XmlDocument xmlDoc = new XmlDocument();
                var fileStream = fileSystem.File.OpenRead(file);
                xmlDoc.Load(fileStream);
                fileStream.Close();
                var xPath = "Project/PropertyGroup/Copyright";
                var copyrightNode = xmlDoc.SelectSingleNode(xPath);
                bool changed = false;

                if (copyrightNode != null && copyrightNode.InnerText != newCopyright.Value)
                {
                    statusWriter?.Write($"Replacing {copyrightNode.InnerText} in {file} with {newCopyright.Value}");
                    copyrightNode.InnerText = newCopyright.Value;
                    changed = true;
                }
                else if (copyrightNode == null)
                {
                    // Create the Copyright node if it doesn't exist
                    var propertyGroupNode = xmlDoc.SelectSingleNode("Project/PropertyGroup");
                    if (propertyGroupNode != null)
                    {
                        copyrightNode = xmlDoc.CreateElement("Copyright");
                        copyrightNode.InnerText = newCopyright.Value;
                        propertyGroupNode.AppendChild(copyrightNode);
                        statusWriter?.Write($"Adding {newCopyright.Value} to {file}");
                        changed = true;
                    }
                }

                if (changed)
                {
                    var fileWriteStream = fileSystem.File.Open(file, FileMode.Truncate, FileAccess.Write);
                    xmlDoc.Save(fileWriteStream);
                    fileWriteStream.Close();
                }
            }
        }

        public static IEnumerable<VersionNumber> Get(string path, IStatusWriter statusWriter)
        {
            var seenIt = new HashSet<string>();
            foreach (var file in Directory.EnumerateFiles(ExtensionMethods.PathExpandCombine(path), "AssemblyInfo.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file, Encoding.UTF8).ToList();
                for (int x = 0; x < lines.Count; x++)
                {
                    if (AssemblyAttribute.TryParse(lines[x], out var attribute))
                    {
                        if (attribute.Name == "AssemblyVersion" || attribute.Name == "AssemblyFileVersion")
                        {
                            // Extract and return the version number.
                            if (VersionNumber.TryParse(attribute.Value, out var currentVersion))
                            {
                                if (seenIt.Contains(currentVersion.ToString()))
                                {
                                    continue;
                                }
                                seenIt.Add(currentVersion.ToString());
                                yield return currentVersion;
                            }
                        }
                    }
                }
            }
            var xPaths = new string[] { "Project/PropertyGroup/Version", "Project/PropertyGroup/FileVersion", "Project/PropertyGroup/InformationalVersion" };
            foreach (var file in Directory.EnumerateFiles(ExtensionMethods.PathExpandCombine(path), "*.csproj", SearchOption.AllDirectories))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);
                foreach (var xPath in xPaths)
                {
                    var version = xmlDoc.SelectSingleNode(xPath);
                    if (version != null)
                    {
                        VersionNumber.TryParse(version.InnerText, out var currentVersion);
                        if (seenIt.Contains(currentVersion.ToString()))
                        {
                            continue;
                        }
                        seenIt.Add(currentVersion.ToString());
                        yield return currentVersion;
                    }
                }
            }
            yield break;
        }

        public static IEnumerable<string> GetCompany(string path, IStatusWriter statusWriter)
        {
            var seenIt = new HashSet<string>();
            foreach (var file in Directory.EnumerateFiles(ExtensionMethods.PathExpandCombine(path), "AssemblyInfo.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file, Encoding.UTF8).ToList();
                for (int x = 0; x < lines.Count; x++)
                {
                    if (AssemblyAttribute.TryParseExact(lines[x], "AssemblyCompany", out var attribute))
                    {
                        // Extract and return the version number.
                        var coName = attribute.Value;
                        if (seenIt.Contains(coName))
                        {
                            continue;
                        }
                        seenIt.Add(coName);
                        yield return coName;
                    }
                }
            }
            foreach (var file in Directory.EnumerateFiles(ExtensionMethods.PathExpandCombine(path), "*.csproj", SearchOption.AllDirectories))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);
                var xPath = "Project/PropertyGroup/Company";
                var company = xmlDoc.SelectSingleNode(xPath);
                if (company != null)
                {
                    if (seenIt.Contains(company.InnerText))
                    {
                        continue;
                    }
                    seenIt.Add(company.InnerText);
                    yield return company.InnerText;
                }
            }
            yield break;
        }

        public static void SetCompany(string path, string company, IStatusWriter statusWriter)
        {
            var newCompany = new AssemblyAttribute("AssemblyCompany", $"{company}");
            foreach (var file in Directory.EnumerateFiles(ExtensionMethods.PathExpandCombine(path), "AssemblyInfo.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file, Encoding.UTF8).ToList();
                for (int x = 0; x < lines.Count; x++)
                {
                    if (AssemblyAttribute.TryParseExact(lines[x], "AssemblyCompany", out var attrib) && attrib.Value != company)
                    {
                        statusWriter?.Write($"Replacing {lines[x]} in {file} with {newCompany}");
                        lines[x] = newCompany.ToString();
                    }
                }
                File.WriteAllLines(file, lines, Encoding.UTF8);
            }
            foreach (var file in Directory.EnumerateFiles(ExtensionMethods.PathExpandCombine(path), "*.csproj", SearchOption.AllDirectories))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);
                var xPath = "Project/PropertyGroup/Company";
                var companyNode = xmlDoc.SelectSingleNode(xPath);
                if (companyNode != null)
                {
                    statusWriter?.Write($"Replacing {companyNode.InnerText} in {file} with {newCompany}");
                    companyNode.InnerText = newCompany.ToString();
                    xmlDoc.Save(file);
                }
            }
        }

        public static IEnumerable<string> GetRecentGitHistory(string path, int daysBack = 7)
        {
            var gitRepo = new Repository(ExtensionMethods.PathExpandCombine(path));
            var cutoffDate = DateTimeOffset.Now.AddDays(-daysBack);
            var commits = gitRepo.Commits
                .Where(c => c.Committer.When >= cutoffDate)
                .OrderByDescending(c => c.Committer.When)
                .ToList();

            foreach (var commit in commits)
            {
                var tags = gitRepo.Tags.Where(t => t.Target.Id == commit.Id).Select(t => t.FriendlyName);
                var tagString = tags.Any() ? $" [{string.Join(", ", tags)}]" : string.Empty;
                yield return $"{commit.Sha.Substring(0, 7)} - {commit.MessageShort}{tagString}";
            }
        }
    }
}
