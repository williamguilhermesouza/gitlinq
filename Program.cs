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

var gitService = new GitService(gitRoot);

while (Prompt(out string input, commands))
{
    if (string.IsNullOrEmpty(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    if (input == "Commits")
    {
        var commits = gitService.GetCommits();
        var table = new Table();
        table.AddColumn("[green]Id[/]");
        table.AddColumn("Message");

        foreach (var commit in commits)
        {
            table.AddRow($"{commit.Id}", $"{commit.Message}");
        }

        AnsiConsole.Write(table);
    }
}

return;

static bool Prompt(out string input, List<ICommand> commands)
{
    var text = ReadLine.Read("gitlinq> ");
    input = text;
    commands.FirstOrDefault(com => text == com.Name || com.Aliases.Contains(text))?.Execute();

    var parsed = QueryParser.ParseExpression(input);
    AnsiConsole.MarkupLine($"parsed: [green]{parsed}[/]");

    return true;
}
