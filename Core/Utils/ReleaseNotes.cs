using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using AssimilationSoftware.Buildster.Core.Utils;

namespace AssimilationSoftware.Buildster.Core.Utils
{
    public class ReleaseNotes
    {
        public string FilePath { get; private set; }
        private readonly IFileSystem fileSystem;

        public ReleaseNotes(string path, IFileSystem fileSystem = null)
        {
            this.fileSystem = fileSystem ?? new FileSystem();
            FilePath = System.Environment.ExpandEnvironmentVariables(path);
            if (!fileSystem.File.Exists(FilePath) && fileSystem.Directory.Exists(FilePath))
            {
                FilePath = fileSystem.Path.Combine(FilePath, "ReadMe.md");
            }
        }

        public void AppendNotes(DateTime? releaseDate, VersionNumber newVersion, string[] releaseNotes)
        {
            AppendNotes(FilePath, releaseDate, newVersion, releaseNotes, fileSystem);
        }

        public static void AppendNotes(string path, DateTime? releaseDate, VersionNumber newVersion, string[] releaseNotes, IFileSystem fileSystem = null)
        {
            if (fileSystem == null)
            {
                fileSystem = new FileSystem();
            }
            var expandedPath = System.Environment.ExpandEnvironmentVariables(path);
            if (!fileSystem.File.Exists(expandedPath) && fileSystem.Directory.Exists(expandedPath))
            {
                expandedPath = fileSystem.Path.Combine(expandedPath, "ReadMe.md");
            }
            var contents = new StringBuilder();
            if (fileSystem.File.Exists(expandedPath) && !fileSystem.File.ReadAllText(expandedPath).EndsWith(System.Environment.NewLine))
            {
                // Start with a new line if the file does not end with one.
                contents.AppendLine();
            }
            contents.AppendLine($"- {releaseDate ?? DateTime.Today:yyyy-MM-dd}: Build {newVersion}");
            if (releaseNotes != null)
            {
                foreach (var line in releaseNotes)
                {
                    contents.Append($"\t- {line.Trim()}");
                    if (line.EndsWith("."))
                    {
                        contents.AppendLine();
                    }
                    else
                    {
                        contents.AppendLine(".");
                    }
                }
            }
            fileSystem.File.AppendAllText(expandedPath, contents.ToString());
        }
    }
}
