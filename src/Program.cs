using GitLinq;
using GitLinq.Commands;
using GitLinq.Services;
using Spectre.Console;

ReadLine.HistoryEnabled = true;
ReadLine.AutoCompletionHandler = new AutoCompletionHandler();
var commands = new List<ICommand>
{
    new Clear()
};

var currentDirectory = Directory.GetCurrentDirectory();
var gitRoot = GitService.FindGitRoot(currentDirectory);

if (gitRoot == null)
    throw new InvalidOperationException("Not inside a Git repository");

var expressionBuilder = new LinqExpressionBuilder(gitRoot);

while (Prompt(out string input))
{
    if (string.IsNullOrEmpty(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    if (TryExecuteCommand(input, commands))
        continue;

    try
    {
        // Parse the input into an AST
        var ast = QueryParser.ParseExpression(input);
        
        // Execute the query
        var result = expressionBuilder.Execute(ast);
        
        // Handle different result types
        DisplayResult(result);
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
    }
}

return;

static void DisplayResult(object? result)
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
            
        case int count:
            AnsiConsole.MarkupLine($"[green]{count}[/]");
            break;
            
        case bool value:
            AnsiConsole.MarkupLine(value ? "[green]true[/]" : "[red]false[/]");
            break;
            
        default:
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(result.ToString() ?? "")}[/]");
            break;
    }
}

static void DisplayCommitsTable(IEnumerable<CommitInfo> commits)
{
    var table = new Table();
    table.Border(TableBorder.Rounded);
    table.AddColumn(new TableColumn("[green]SHA[/]").Width(10));
    table.AddColumn(new TableColumn("[blue]Author[/]").Width(20));
    table.AddColumn(new TableColumn("[yellow]Date[/]").Width(20));
    table.AddColumn("Message");

    var count = 0;
    foreach (var commit in commits)
    {
        table.AddRow(
            Markup.Escape(commit.Sha[..7]),
            Markup.Escape(commit.AuthorName),
            commit.AuthorWhen.ToString("yyyy-MM-dd HH:mm"),
            Markup.Escape(commit.MessageShort)
        );
        count++;
    }

    AnsiConsole.Write(table);
    AnsiConsole.MarkupLine($"[dim]({count} commits)[/]");
}

static bool Prompt(out string input)
{
    var text = ReadLine.Read("gitlinq> ");
    input = text;

    return true;
}

static bool TryExecuteCommand(string input, List<ICommand> commands)
{
    var command = commands.FirstOrDefault(com => input == com.Name || com.Aliases.Contains(input));
    if (command != null)
    {
        command.Execute();
        return true;
    }

    return false;
}