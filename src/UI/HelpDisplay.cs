using Spectre.Console;

namespace GitLinq.UI;

/// <summary>
/// Handles displaying help information to the console.
/// </summary>
public static class HelpDisplay
{
    /// <summary>
    /// Print the CLI help message.
    /// </summary>
    public static void PrintHelp()
    {
        AnsiConsole.Write(new Panel(
            new Rows(
                new Markup("[bold]Usage:[/]"),
                new Text("  gitlinq                      Start interactive mode"),
                new Text("  gitlinq -q <query>           Execute a single query and exit"),
                new Text("  gitlinq -h, --help           Show this help message"),
                Text.Empty,
                new Markup("[bold]Examples:[/]"),
                new Text("  gitlinq -q \"Commits.Take(10)\""),
                new Text("  gitlinq -q \"Commits.Where(c => c.Message.Contains(\\\"fix\\\"))\""),
                new Text("  gitlinq -q \"Commits.Count()\""),
                Text.Empty,
                new Markup("[bold]Environment Variables:[/]"),
                new Text("  GITLINQ_DEBUG=1              Enable debug mode for troubleshooting")
            ))
        {
            Header = new PanelHeader("[bold]GitLinq[/] - Query git commits using LINQ-like syntax"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 1)
        });
    }
}
