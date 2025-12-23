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
    
    var parsed = QueryParser.ParseExpression(input);
    // AnsiConsole.MarkupLine($"parsed: [green]{parsed}[/]");

    // if (input == "Commits")
    // {
        var commits = gitService.GetCommits();
        var expression = LinqExpressionBuilder.BuildExpression<Commit>(parsed);
        var result = commits.AsQueryable().Where(expression).ToList();
        commits = result;
        
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
