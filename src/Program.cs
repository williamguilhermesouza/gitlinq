using GitLinq;
using GitLinq.Commands;
using GitLinq.Services;
using Spectre.Console;
using Spectre.Console.Rendering;
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
        DisplayResult = result => DisplayResult(result)
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
        
        // Extract search text if this is a content search query
        var contentSearchText = ExtractContentSearchText(sanitizedQuery);
        
        var ast = QueryParser.ParseExpression(sanitizedQuery);
        var result = expressionBuilder.Execute(ast);
        DisplayResult(result, contentSearchText);
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

void DisplayResult(object? result, string? contentSearchText = null)
{
    switch (result)
    {
        case null:
            AnsiConsole.MarkupLine("[dim](null)[/]");
            break;
        case IEnumerable<CommitInfo> commits:
            DisplayCommitsTable(commits, contentSearchText);
            break;
        case CommitInfo commit:
            DisplayCommitsTable([commit], contentSearchText);
            break;
        case IEnumerable<FileChange> files:
            DisplayFilesTable(files);
            break;
        case FileChange file:
            DisplayFilesTable([file]);
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

void DisplayCommitsTable(IEnumerable<CommitInfo> commits, string? contentSearchText = null)
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
        .AddColumn(new TableColumn("[blue]Author[/]").Width(15))
        .AddColumn(new TableColumn("[yellow]Date[/]").Width(17))
        .AddColumn(new TableColumn("[cyan]Files[/]").Width(6).RightAligned())
        .AddColumn(new TableColumn("[green]+[/]").Width(7).RightAligned())
        .AddColumn(new TableColumn("[red]-[/]").Width(7).RightAligned())
        .AddColumn("Message");

    foreach (var commit in commitList)
    {
        var authorDisplay = commit.AuthorName.Length > 15 
            ? commit.AuthorName[..12] + "..." 
            : commit.AuthorName;
        var messageDisplay = commit.MessageShort.Length > 35 
            ? commit.MessageShort[..32] + "..." 
            : commit.MessageShort;
            
        table.AddRow(
            Markup.Escape(commit.Sha[..7]),
            Markup.Escape(authorDisplay),
            commit.AuthorWhen.ToString("yyyy-MM-dd HH:mm"),
            commit.Diff.FilesChanged.ToString(),
            $"[green]+{commit.Diff.TotalLinesAdded}[/]",
            $"[red]-{commit.Diff.TotalLinesDeleted}[/]",
            Markup.Escape(messageDisplay)
        );
    }

    AnsiConsole.Write(table);
    AnsiConsole.MarkupLine($"[dim]({commitList.Count} commit{(commitList.Count != 1 ? "s" : "")})[/]");
    
    // If this is a content search, display matching lines with context
    if (!string.IsNullOrEmpty(contentSearchText))
    {
        DisplayContentMatches(commitList, contentSearchText);
    }
}

void DisplayFilesTable(IEnumerable<FileChange> files)
{
    var fileList = files.ToList();
    
    if (fileList.Count == 0)
    {
        AnsiConsole.MarkupLine("[dim]No files found.[/]");
        return;
    }

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn(new TableColumn("[yellow]Status[/]").Width(10))
        .AddColumn(new TableColumn("[green]+[/]").Width(6).RightAligned())
        .AddColumn(new TableColumn("[red]-[/]").Width(6).RightAligned())
        .AddColumn("Path");

    foreach (var file in fileList)
    {
        var statusColor = file.Status switch
        {
            "Added" => "green",
            "Deleted" => "red",
            "Modified" => "yellow",
            "Renamed" => "cyan",
            _ => "white"
        };
        
        var path = file.OldPath != null 
            ? $"{file.OldPath} → {file.Path}" 
            : file.Path;

        table.AddRow(
            $"[{statusColor}]{file.Status}[/]",
            $"[green]+{file.LinesAdded}[/]",
            $"[red]-{file.LinesDeleted}[/]",
            Markup.Escape(path)
        );
    }

    AnsiConsole.Write(table);
    AnsiConsole.MarkupLine($"[dim]({fileList.Count} file{(fileList.Count != 1 ? "s" : "")})[/]");
}

// ============== Debug Functions ==============

/// <summary>
/// Extract the search text from content search queries like AddedContains, DeletedContains, ContentContains.
/// Supports both double quotes ("text") and single quotes ('text').
/// </summary>
string? ExtractContentSearchText(string query)
{
    // Look for patterns like: AddedContains("text"), DeletedContains("text"), ContentContains("text")
    var patterns = new[] { "AddedContains", "DeletedContains", "ContentContains" };
    
    foreach (var pattern in patterns)
    {
        var index = query.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            // Find the opening parenthesis and extract the string
            var parenStart = query.IndexOf('(', index);
            if (parenStart >= 0)
            {
                // Try double quotes first
                var doubleQuoteStart = query.IndexOf('"', parenStart);
                if (doubleQuoteStart >= 0)
                {
                    var doubleQuoteEnd = query.IndexOf('"', doubleQuoteStart + 1);
                    if (doubleQuoteEnd > doubleQuoteStart)
                    {
                        return query[(doubleQuoteStart + 1)..doubleQuoteEnd];
                    }
                }
                
                // Try single quotes
                var singleQuoteStart = query.IndexOf('\'', parenStart);
                if (singleQuoteStart >= 0)
                {
                    var singleQuoteEnd = query.IndexOf('\'', singleQuoteStart + 1);
                    if (singleQuoteEnd > singleQuoteStart)
                    {
                        return query[(singleQuoteStart + 1)..singleQuoteEnd];
                    }
                }
            }
        }
    }
    
    return null;
}

/// <summary>
/// Display matching content lines with context for content search queries.
/// </summary>
void DisplayContentMatches(List<CommitInfo> commits, string searchText)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[bold cyan]Matching lines for '[yellow]{Markup.Escape(searchText)}[/]':[/]");
    AnsiConsole.WriteLine();
    
    var totalMatches = 0;
    
    foreach (var commit in commits)
    {
        var commitMatches = new List<(string filePath, MatchedLine match)>();
        
        foreach (var file in commit.Diff.Files)
        {
            var matches = file.GetContentMatches(searchText);
            foreach (var match in matches)
            {
                commitMatches.Add((file.Path, match));
            }
        }
        
        if (commitMatches.Count > 0)
        {
            // Create a panel for each commit with matches
            var content = new List<IRenderable>();
            content.Add(new Markup($"[green]{commit.Sha[..7]}[/] [dim]{Markup.Escape(commit.MessageShort)}[/]"));
            content.Add(new Text(""));
            
            foreach (var (filePath, match) in commitMatches)
            {
                var typeColor = match.MatchType == "added" ? "green" : "red";
                var typeSymbol = match.MatchType == "added" ? "+" : "-";
                
                content.Add(new Markup($"  [dim]{Markup.Escape(filePath)}[/] [bold {typeColor}]({typeSymbol})[/]"));
                
                foreach (var (line, isMatch) in match.ContextLines)
                {
                    var lineContent = line.Length > 80 ? line[..77] + "..." : line;
                    if (isMatch)
                    {
                        // Highlight the matching line and the search text within it
                        var highlighted = HighlightSearchText(lineContent, searchText, typeColor);
                        content.Add(new Markup($"    [bold {typeColor}]→[/] {highlighted}"));
                    }
                    else
                    {
                        content.Add(new Markup($"      [dim]{Markup.Escape(lineContent)}[/]"));
                    }
                }
                content.Add(new Text(""));
                totalMatches++;
            }
            
            var panel = new Panel(new Rows(content))
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0)
            };
            AnsiConsole.Write(panel);
        }
    }
    
    AnsiConsole.MarkupLine($"[dim]({totalMatches} matching location{(totalMatches != 1 ? "s" : "")})[/]");
}

/// <summary>
/// Highlight search text within a line.
/// </summary>
string HighlightSearchText(string line, string searchText, string color)
{
    var escapedLine = Markup.Escape(line);
    var index = line.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
    
    if (index >= 0)
    {
        var before = Markup.Escape(line[..index]);
        var match = Markup.Escape(line.Substring(index, searchText.Length));
        var after = Markup.Escape(line[(index + searchText.Length)..]);
        return $"{before}[bold yellow on {color}]{match}[/]{after}";
    }
    
    return escapedLine;
}

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