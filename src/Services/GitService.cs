using GitLinq.Models;
using LibGit2Sharp;

namespace GitLinq.Services;

/// <summary>
/// Service for interacting with Git repositories.
/// </summary>
internal class GitService
{
    private readonly string _repositoryPath;
    
    public GitService(string repositoryPath)
    {
        _repositoryPath = repositoryPath;
    }

    public List<CommitInfo> GetCommits()
    {
        using var repository = new Repository(_repositoryPath);
        var result = new List<CommitInfo>();

        foreach (var commit in repository.Commits)
        {
            var commitInfo = new CommitInfo
            {
                Sha = commit.Sha,
                Message = commit.Message.TrimEnd(),
                MessageShort = commit.MessageShort.TrimEnd(),
                AuthorName = commit.Author.Name,
                AuthorEmail = commit.Author.Email,
                AuthorWhen = commit.Author.When,
                CommitterName = commit.Committer.Name,
                CommitterEmail = commit.Committer.Email,
                CommitterWhen = commit.Committer.When,
                Diff = new DiffData
                {
                    Files = GetFileChanges(repository, commit)
                }
            };
            result.Add(commitInfo);
        }

        return result;
    }

    private List<FileChange> GetFileChanges(Repository repository, Commit commit)
    {
        var changes = new List<FileChange>();

        // Compare with parent commit, or empty tree if no parent (initial commit)
        var parent = commit.Parents.FirstOrDefault();
        var parentTree = parent?.Tree;
        
        var patch = repository.Diff.Compare<Patch>(parentTree, commit.Tree);

        foreach (var entry in patch)
        {
            var fileChange = new FileChange
            {
                Path = entry.Path,
                OldPath = entry.OldPath != entry.Path ? entry.OldPath : null,
                Status = entry.Status.ToString(),
                LinesAdded = entry.LinesAdded,
                LinesDeleted = entry.LinesDeleted,
                IsBinary = entry.IsBinaryComparison
            };
            
            // Extract actual diff content (added and deleted lines)
            if (!entry.IsBinaryComparison)
            {
                ExtractDiffContent(entry.Patch, fileChange);
            }
            
            changes.Add(fileChange);
        }

        return changes;
    }
    
    /// <summary>
    /// Parses the patch text to extract added and deleted line content.
    /// </summary>
    private void ExtractDiffContent(string patchText, FileChange fileChange)
    {
        if (string.IsNullOrEmpty(patchText))
            return;
            
        var lines = patchText.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.StartsWith('+') && !line.StartsWith("+++"))
            {
                // Added line - remove the '+' prefix
                fileChange.AddedContent.Add(line.Length > 1 ? line[1..] : "");
            }
            else if (line.StartsWith('-') && !line.StartsWith("---"))
            {
                // Deleted line - remove the '-' prefix
                fileChange.DeletedContent.Add(line.Length > 1 ? line[1..] : "");
            }
        }
    }

    public static string? FindGitRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);

        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;

            dir = dir.Parent;
        }

        return null;
    }
}
