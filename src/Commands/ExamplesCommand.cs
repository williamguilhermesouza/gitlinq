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
        
        var examples = new (string Query, string Description)[]
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

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[green]Query[/]");
        table.AddColumn("Description");

        foreach (var (query, description) in examples)
        {
            table.AddRow(Markup.Escape(query), description);
        }

        AnsiConsole.Write(table);
    }
}
