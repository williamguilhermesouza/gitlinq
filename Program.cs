using GitLinq;
using GitLinq.Commands;
using Spectre.Console;

ReadLine.HistoryEnabled = true;
ReadLine.AutoCompletionHandler = new AutoCompletionHandler();
var commands = new List<ICommand>
{
    new Clear()
};

while (Prompt(out string input, commands))
{
    if (string.IsNullOrEmpty(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;
}

return;

static bool Prompt(out string input, List<ICommand> commands)
{
    var text = ReadLine.Read("gitlinq> ");
    input = text;
    commands.FirstOrDefault(com => text == com.Name || com.Aliases.Contains(text))?.Execute();
    
    AnsiConsole.MarkupLine($"[green] typed: [/] {input}");
    return true;
}
