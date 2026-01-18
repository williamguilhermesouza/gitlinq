using Spectre.Console;

namespace GitLinq.Commands;

public class ExamplesCommand : ICommand
{
    public string Name => "examples";
    public string Description => "Show example queries";
    public IReadOnlyList<string> Aliases => ["ex", "samples"];
    
    public void Execute(string[] args, CommandContext context)
    {
        AnsiConsole.MarkupLine("[bold]Example Queries:[/]\n");
        
        var commitExamples = new (string Query, string Description)[]
        {
            ("Commits", "Get all commits"),
            ("Commits.Take(10)", "Get the first 10 commits"),
            ("Commits.Skip(5).Take(10)", "Pagination: skip 5, take 10"),
            ("Commits.First()", "Get the most recent commit"),
            ("Commits.Count()", "Count total commits"),
            ("Commits.Where(c => c.Message.Contains(\"fix\"))", "Find commits with 'fix' in message"),
            ("Commits.Where(c => c.AuthorName.Contains(\"Alice\"))", "Find commits by author"),
            ("Commits.Where(c => c.Message.StartsWith(\"feat\"))", "Find commits starting with 'feat'"),
            ("Commits.First(c => c.Message.Contains(\"bug\"))", "Find first commit mentioning 'bug'"),
            ("Commits.Any(c => c.Message.Contains(\"hotfix\"))", "Check if any hotfix commits exist"),
            ("Commits.Count(c => c.AuthorName.Contains(\"Bob\"))", "Count commits by Bob"),
        };

        var diffExamples = new (string Query, string Description)[]
        {
            ("Commits.Where(c => c.Diff.FilesChanged > 5)", "Commits that changed more than 5 files"),
            ("Commits.Where(c => c.Diff.TotalLinesAdded > 100)", "Commits with more than 100 lines added"),
            ("Commits.Where(c => c.Diff.Files.Any(f => f.Path.Contains(\".cs\")))", "Commits that modified C# files"),
            ("Commits.First().Diff.Files", "Get files changed in the most recent commit"),
            ("Commits.Where(c => c.Diff.Files.Any(f => f.Status == \"Added\"))", "Commits that added new files"),
        };

        var diffContentExamples = new (string Query, string Description)[]
        {
            ("Commits.Where(c => c.Diff.Files.Any(f => f.AddedContains(\"TODO\")))", "Find commits that added 'TODO'"),
            ("Commits.Where(c => c.Diff.Files.Any(f => f.DeletedContains(\"bug\")))", "Find commits that removed 'bug'"),
            ("Commits.Where(c => c.Diff.Files.Any(f => f.ContentContains(\"password\")))", "Find commits that touched 'password'"),
            ("Commits.First().Diff.Files.First().AddedContent", "View added lines in most recent file change"),
        };

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[green]Query[/]");
        table.AddColumn("Description");

        table.AddRow("[bold cyan]— Commit Queries —[/]", "");
        foreach (var (query, description) in commitExamples)
        {
            table.AddRow(Markup.Escape(query), description);
        }
        
        table.AddRow("", "");
        table.AddRow("[bold cyan]— Diff Queries —[/]", "");
        foreach (var (query, description) in diffExamples)
        {
            table.AddRow(Markup.Escape(query), description);
        }

        table.AddRow("", "");
        table.AddRow("[bold cyan]— Diff Content Queries —[/]", "");
        foreach (var (query, description) in diffContentExamples)
        {
            table.AddRow(Markup.Escape(query), description);
        }

        AnsiConsole.Write(table);
    }
}
