using LibGit2Sharp;

namespace GitLinq.Services
{
    internal class GitService
    {
        private string _repositoryPath;
        public GitService(string repositoryPath)
        {
            _repositoryPath = repositoryPath;
        }

        public IEnumerable<Commit> GetCommits()
        {
            using var repository = new Repository(_repositoryPath);

            foreach (var commit in repository.Commits)
                yield return commit;
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
