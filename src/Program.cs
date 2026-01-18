using GitLinq;
using GitLinq.Commands;
using GitLinq.Services;
using Spectre.Console;
using System.Runtime.InteropServices;
using System.Text;

// Set console encoding to UTF-8 for cross-platform compatibility
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Check for debug mode (set GITLINQ_DEBUG=1 to enable)
var isDebugMode = Environment.GetEnvironmentVariable("GITLINQ_DEBUG") == "1";

if (isDebugMode)
    PrintDebugEnvironmentInfo();

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

        if (!commands.TryExecute(input, context))
            ExecuteQuery(input);
    }
}

bool ExecuteQuery(string query)
{
    try
    {
        if (isDebugMode)
            PrintDebugInputInfo(query, "before sanitization");
        
        // Fix: ReadLine on some Windows terminals inserts null bytes
        var sanitizedQuery = query.Replace("\0", "");
        
        if (isDebugMode && sanitizedQuery != query)
        {
            AnsiConsole.MarkupLine("[yellow]Debug:[/] Null bytes were removed from input");
            PrintDebugInputInfo(sanitizedQuery, "after sanitization");
        }
        
        var ast = QueryParser.ParseExpression(sanitizedQuery);
        var result = expressionBuilder.Execute(ast);
        DisplayResult(result);
        return true;
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        if (isDebugMode)
        {
            AnsiConsole.MarkupLine($"[dim]Debug - Exception type: {ex.GetType().Name}[/]");
            if (ex.InnerException != null)
                AnsiConsole.MarkupLine($"[dim]Debug - Inner exception: {Markup.Escape(ex.InnerException.Message)}[/]");
        }
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

// ============== Debug Functions ==============

void PrintDebugEnvironmentInfo()
{
    var panel = new Panel(
        new Rows(
            new Markup($"[bold]GitLinq Debug Mode[/]"),
            new Text(""),
            new Markup($"[dim]OS:[/] {RuntimeInformation.OSDescription}"),
            new Markup($"[dim]Architecture:[/] {RuntimeInformation.OSArchitecture}"),
            new Markup($"[dim].NET Runtime:[/] {RuntimeInformation.FrameworkDescription}"),
            new Text(""),
            new Markup($"[dim]Console.InputEncoding:[/] {Console.InputEncoding.EncodingName} (CodePage: {Console.InputEncoding.CodePage})"),
            new Markup($"[dim]Console.OutputEncoding:[/] {Console.OutputEncoding.EncodingName} (CodePage: {Console.OutputEncoding.CodePage})"),
            new Text(""),
            new Markup($"[dim]Terminal:[/] {GetTerminalInfo()}"),
            new Markup($"[dim]Working Directory:[/] {Environment.CurrentDirectory}"),
            new Text(""),
            new Markup("[dim]Set GITLINQ_DEBUG=0 or unset to disable debug mode[/]")
        ))
    {
        Header = new PanelHeader("[yellow]Debug Info[/]"),
        Border = BoxBorder.Rounded,
        Padding = new Padding(1, 0)
    };
    
    AnsiConsole.Write(panel);
    AnsiConsole.WriteLine();
}

void PrintDebugInputInfo(string input, string label)
{
    var bytes = Encoding.UTF8.GetBytes(input);
    var hexBytes = string.Join(" ", bytes.Select(b => b.ToString("X2")));
    var hasNullBytes = bytes.Contains((byte)0);
    var hasNonAscii = bytes.Any(b => b > 127);
    
    AnsiConsole.MarkupLine($"[dim]Debug - Input ({label}):[/]");
    AnsiConsole.MarkupLine($"[dim]  String: {Markup.Escape(input)}[/]");
    AnsiConsole.MarkupLine($"[dim]  Length: {input.Length} chars, {bytes.Length} bytes[/]");
    AnsiConsole.MarkupLine($"[dim]  Bytes: {hexBytes}[/]");
    
    if (hasNullBytes)
        AnsiConsole.MarkupLine("[yellow]  Warning: Input contains null bytes (0x00)[/]");
    if (hasNonAscii)
        AnsiConsole.MarkupLine("[yellow]  Warning: Input contains non-ASCII characters[/]");
}

string GetTerminalInfo()
{
    var term = Environment.GetEnvironmentVariable("TERM") ?? "not set";
    var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? "";
    var wtSession = Environment.GetEnvironmentVariable("WT_SESSION");
    var conEmu = Environment.GetEnvironmentVariable("ConEmuANSI");
    
    if (!string.IsNullOrEmpty(wtSession))
        return "Windows Terminal";
    if (!string.IsNullOrEmpty(conEmu))
        return "ConEmu";
    if (!string.IsNullOrEmpty(termProgram))
        return termProgram;
    if (Environment.GetEnvironmentVariable("VSCODE_INJECTION") != null)
        return "VS Code Integrated Terminal";
        
    // Check for PowerShell or CMD
    var psVersion = Environment.GetEnvironmentVariable("PSVersionTable");
    if (psVersion != null || Environment.GetEnvironmentVariable("PSModulePath") != null)
        return "PowerShell";
    
    return term != "not set" ? term : "Unknown (likely CMD or basic console)";
}