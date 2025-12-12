using LibGit2Sharp;

using var repo = new Repository(".");

foreach (var commit in repo.Commits.Take((15)))
{
    Console.WriteLine($"Id: {commit.Id}");
}