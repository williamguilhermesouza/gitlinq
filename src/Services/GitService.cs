using LibGit2Sharp;

namespace GitLinq.Services
{
    /// <summary>
    /// Represents the diff data for a commit (files changed, lines added/deleted).
    /// </summary>
    public class DiffData
    {
        /// <summary>
        /// List of file changes in this commit.
        /// </summary>
        public List<FileChange> Files { get; set; } = new();
        
        /// <summary>
        /// Total lines added across all files.
        /// </summary>
        public int TotalLinesAdded => Files.Sum(f => f.LinesAdded);
        
        /// <summary>
        /// Total lines deleted across all files.
        /// </summary>
        public int TotalLinesDeleted => Files.Sum(f => f.LinesDeleted);
        
        /// <summary>
        /// Number of files changed.
        /// </summary>
        public int FilesChanged => Files.Count;
        
        /// <summary>
        /// Check if any file in this diff has added lines containing the specified text.
        /// </summary>
        public bool AddedContains(string text) => 
            Files.Any(f => f.AddedContains(text));
        
        /// <summary>
        /// Check if any file in this diff has deleted lines containing the specified text.
        /// </summary>
        public bool DeletedContains(string text) => 
            Files.Any(f => f.DeletedContains(text));
        
        /// <summary>
        /// Check if any file in this diff has changed lines (added or deleted) containing the specified text.
        /// </summary>
        public bool ContentContains(string text) => 
            Files.Any(f => f.ContentContains(text));
    }

    public class CommitInfo
    {
        public string Sha { get; set; } = "";
        public string Message { get; set; } = "";
        public string MessageShort { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public string AuthorEmail { get; set; } = "";
        public DateTimeOffset AuthorWhen { get; set; }
        public string CommitterName { get; set; } = "";
        public string CommitterEmail { get; set; } = "";
        public DateTimeOffset CommitterWhen { get; set; }
        
        /// <summary>
        /// The diff data for this commit (files changed, lines added/deleted, content).
        /// </summary>
        public DiffData Diff { get; set; } = new();
    }

    internal class GitService
    {
        private string _repositoryPath;
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
}
