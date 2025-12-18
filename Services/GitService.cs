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

        public List<Commit> GetCommits()
        {
            using var repository = new Repository(_repositoryPath);

            return repository.Commits.ToList();
        }
    }
}
