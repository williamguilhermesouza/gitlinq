using GitLinq.Models;
using LibGit2Sharp;

namespace GitLinq.Services;

public static class GitService
{
    public static IEnumerable<CommitModel> GetCommits(string repoPath)
    {
        using var repository = new Repository(repoPath);
        foreach (var c in repository.Commits)
        {
            yield return new CommitModel
            {
                Id = c.Sha ?? string.Empty,
                Message = c.MessageShort ?? string.Empty,
                AuthorName = c.Author?.Name ?? string.Empty,
                When = c.Author?.When ?? DateTimeOffset.MinValue
            };
        }
    }
    
}