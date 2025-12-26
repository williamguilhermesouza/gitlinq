using GitLinq;
using GitLinq.Commands;
using GitLinq.Services;
using Spectre.Console;
using LibGit2Sharp;
using GitLinq.AST;
using System.Linq.Expressions;

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

var gitService = new GitService(gitRoot);

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

        var parsed = QueryParser.ParseExpression(input);
        PrintNode(parsed);
        // AnsiConsole.MarkupLine($"parsed: [green]{parsed}[/]");

        // if (input == "Commits")
        // {
        var commits = gitService.GetCommits();
        // var expression = LinqExpressionBuilder.BuildExpression(parsed);
        // var result = commits.AsQueryable().Where(expression).ToList();
        // commits = result;

        var table = new Table();
        table.AddColumn("[green]Id[/]");
        table.AddColumn("Message");

        foreach (var commit in commits)
        {
            table.AddRow($"{commit.Id}", $"{commit.Message}");
        }

        AnsiConsole.Write(table);
        // }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        AnsiConsole.MarkupLine($"[red]Stack Trace:[/] {Markup.Escape(ex.StackTrace ?? string.Empty)}");
    }
}

return;

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

static void PrintNode(BaseNode parsed)
{
    if (parsed is IdentifierNode id)
    {
        AnsiConsole.MarkupLine($"Identifier: [green]{id.Name}[/]");
    }
    else if (parsed is MemberAccessNode member)
    {
        AnsiConsole.MarkupLine($"Member Access: [green]{member.Member}[/]");
        PrintNode(member.Target);
    }
    else if (parsed is StringLiteralNode str)
    {
        AnsiConsole.MarkupLine($"String Literal: [green]{str.Value}[/]");
    }
    else if (parsed is MethodCallNode call)
    {
        AnsiConsole.MarkupLine($"Method Call: [green]{call.Method}[/]");
        PrintNode(call.Target);
        foreach (var arg in call.Arguments)
        {
            PrintNode(arg);
        }
    }
    else if (parsed is LambdaNode lambda)
    {
        AnsiConsole.MarkupLine($"Lambda: [green]{lambda.Parameter}[/]");
        PrintNode(lambda.Body);
    }
}