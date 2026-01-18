using GitLinq;
using GitLinq.Commands;
using GitLinq.Services;
using Spectre.Console;
using System.Text;

// Set console encoding to UTF-8 for cross-platform compatibility
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var gitRoot = GitService.FindGitRoot(Directory.GetCurrentDirectory());

if (gitRoot == null)
{
    AnsiConsole.MarkupLine("[red]Error:[/] Not inside a Git repository");
    return 1;
}

var expressionBuilder = new LinqExpressionBuilder(gitRoot);

// Handle command line arguments
if (args.Length > 0)
    return HandleCommandLineArgs(args);

// Interactive mode
RunInteractiveMode();
return 0;

int HandleCommandLineArgs(string[] cliArgs)
{
    string? query = null;
    var showHelp = false;
    var hasError = false;

    for (var i = 0; i < cliArgs.Length; i++)
    {
        switch (cliArgs[i])
        {
            case "-q" or "--query" when i + 1 < cliArgs.Length:
                query = cliArgs[++i];
                break;
            case "-q" or "--query":
                AnsiConsole.MarkupLine("[red]Error:[/] -q/--query requires a query argument");
                return 1;
            case "-h" or "--help":
                showHelp = true;
                break;
            default:
                AnsiConsole.MarkupLine($"[red]Error:[/] Unknown argument: {Markup.Escape(cliArgs[i])}");
                hasError = true;
                break;
        }
    }

    if (showHelp || hasError)
    {
        PrintHelp();
        return hasError ? 1 : 0;
    }

    if (query == null) return 0;
    
    return ExecuteQuery(query) ? 0 : 1;
}

void RunInteractiveMode()
{
    ReadLine.HistoryEnabled = true;
    ReadLine.AutoCompletionHandler = new AutoCompletionHandler();

    var commands = new CommandRegistry().DiscoverCommands();
    var context = new CommandContext
    {
        ExpressionBuilder = expressionBuilder,
        Commands = commands,
        DisplayResult = DisplayResult
    };

    AnsiConsole.MarkupLine("[bold]GitLinq[/] - Query git commits using LINQ-like syntax");
    AnsiConsole.MarkupLine("[dim]Type 'help' for available commands or enter a query.[/]\n");

    while (!context.ShouldExit)
    {
        var input = ReadLine.Read("gitlinq> ");
        
        if (string.IsNullOrWhiteSpace(input))
            continue;

        // Fix: ReadLine on some Windows terminals inserts null bytes
        input = input.Replace("\0", "");

        if (!commands.TryExecute(input, context))
            ExecuteQuery(input);
    }
}

bool ExecuteQuery(string query)
{
    try
    {
        var ast = QueryParser.ParseExpression(query);
        var result = expressionBuilder.Execute(ast);
        DisplayResult(result);
        return true;
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        return false;
    }
}

void PrintHelp()
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
            new Text("  gitlinq -q \"Commits.Count()\"")
        ))
    {
        Header = new PanelHeader("[bold]GitLinq[/] - Query git commits using LINQ-like syntax"),
        Border = BoxBorder.Rounded,
        Padding = new Padding(2, 1)
    });
}

void DisplayResult(object? result)
{
    switch (result)
    {
        case null:
            AnsiConsole.MarkupLine("[dim](null)[/]");
            break;
        case IEnumerable<CommitInfo> commits:
            DisplayCommitsTable(commits);
            break;
        case CommitInfo commit:
            DisplayCommitsTable([commit]);
            break;
        case int or long or bool:
            var color = result is bool b ? (b ? "green" : "red") : "green";
            AnsiConsole.MarkupLine($"[{color}]{result}[/]");
            break;
        default:
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(result.ToString() ?? "")}[/]");
            break;
    }
}

void DisplayCommitsTable(IEnumerable<CommitInfo> commits)
{
    var commitList = commits.ToList();
    
    if (commitList.Count == 0)
    {
        AnsiConsole.MarkupLine("[dim]No commits found.[/]");
        return;
    }

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn(new TableColumn("[green]SHA[/]").Width(10))
        .AddColumn(new TableColumn("[blue]Author[/]").Width(20))
        .AddColumn(new TableColumn("[yellow]Date[/]").Width(20))
        .AddColumn("Message");

    foreach (var commit in commitList)
    {
        table.AddRow(
            Markup.Escape(commit.Sha[..7]),
            Markup.Escape(commit.AuthorName),
            commit.AuthorWhen.ToString("yyyy-MM-dd HH:mm"),
            Markup.Escape(commit.MessageShort)
        );
    }

    AnsiConsole.Write(table);
    AnsiConsole.MarkupLine($"[dim]({commitList.Count} commit{(commitList.Count != 1 ? "s" : "")})[/]");
}