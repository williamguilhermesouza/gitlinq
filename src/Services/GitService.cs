using LibGit2Sharp;

namespace GitLinq.Services
{
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

            return repository.Commits.Select(c => new CommitInfo
            {
                Sha = c.Sha,
                Message = c.Message.TrimEnd(),
                MessageShort = c.MessageShort.TrimEnd(),
                AuthorName = c.Author.Name,
                AuthorEmail = c.Author.Email,
                AuthorWhen = c.Author.When,
                CommitterName = c.Committer.Name,
                CommitterEmail = c.Committer.Email,
                CommitterWhen = c.Committer.When
            }).ToList();
        }

        /// <summary>
        /// Gets all commits with their file change information (diffs).
        /// </summary>
        public List<CommitDiff> GetCommitDiffs()
        {
            using var repository = new Repository(_repositoryPath);
            var result = new List<CommitDiff>();

            foreach (var commit in repository.Commits)
            {
                var commitDiff = new CommitDiff
                {
                    Sha = commit.Sha,
                    Message = commit.Message.TrimEnd(),
                    MessageShort = commit.MessageShort.TrimEnd(),
                    AuthorName = commit.Author.Name,
                    AuthorEmail = commit.Author.Email,
                    AuthorWhen = commit.Author.When,
                    Files = GetFileChanges(repository, commit)
                };
                result.Add(commitDiff);
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
                changes.Add(new FileChange
                {
                    Path = entry.Path,
                    OldPath = entry.OldPath != entry.Path ? entry.OldPath : null,
                    Status = entry.Status.ToString(),
                    LinesAdded = entry.LinesAdded,
                    LinesDeleted = entry.LinesDeleted,
                    IsBinary = entry.IsBinaryComparison
                });
            }

            return changes;
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
