using AssimilationSoftware.Buildster.Core.Interfaces;
using LibGit2Sharp;

namespace AssimilationSoftware.Buildster.Core.Utils;

public static class GitUtils
{
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

    public static IEnumerable<string> GetRecentGitHistory(string path, int daysBack = 7)
    {
        using var gitRepo = new Repository(ExtensionMethods.PathExpandCombine(path));

        // Retrieve the main branch (or master as fallback if needed)
        var mainBranch = gitRepo.Branches["main"] ?? gitRepo.Branches["master"];
        if (mainBranch == null)
        {
            yield break;
        }

        var cutoffDate = DateTimeOffset.Now.AddDays(-daysBack);

        // Query commits starting from the tip of main branch
        var commits = mainBranch.Commits
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