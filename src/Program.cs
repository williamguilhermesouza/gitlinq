using GitLinq;
using GitLinq.Commands;
using GitLinq.Diagnostics;
using GitLinq.Services;
using GitLinq.UI;
using Spectre.Console;
using System.Text;

// Set console encoding to UTF-8 for cross-platform compatibility
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Check for debug mode (set GITLINQ_DEBUG=1 to enable)
var isDebugMode = Environment.GetEnvironmentVariable("GITLINQ_DEBUG") == "1";

if (isDebugMode)
    DebugHelper.PrintEnvironmentInfo();

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
        HelpDisplay.PrintHelp();
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
        DisplayResult = result => ResultDisplay.Display(result)
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
            DebugHelper.PrintInputInfo(query, "before sanitization");
        
        // Fix: ReadLine on some Windows terminals inserts null bytes
        var sanitizedQuery = query.Replace("\0", "");
        
        if (isDebugMode && sanitizedQuery != query)
        {
            AnsiConsole.MarkupLine("[yellow]Debug:[/] Null bytes were removed from input");
            DebugHelper.PrintInputInfo(sanitizedQuery, "after sanitization");
        }
        
        // Extract search text if this is a content search query
        var contentSearchText = ResultDisplay.ExtractContentSearchText(sanitizedQuery);
        
        var ast = QueryParser.ParseExpression(sanitizedQuery);
        var result = expressionBuilder.Execute(ast);
        ResultDisplay.Display(result, contentSearchText);
        return true;
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        if (isDebugMode)
            DebugHelper.PrintException(ex);
        return false;
    }
}