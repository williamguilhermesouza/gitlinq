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
                Message = c.Message,
                MessageShort = c.MessageShort,
                AuthorName = c.Author.Name,
                AuthorEmail = c.Author.Email,
                AuthorWhen = c.Author.When,
                CommitterName = c.Committer.Name,
                CommitterEmail = c.Committer.Email,
                CommitterWhen = c.Committer.When
            }).ToList();
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
