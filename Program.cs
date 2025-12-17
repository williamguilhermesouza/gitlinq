using Spectre.Console;

ReadLine.HistoryEnabled = true;

while (Prompt(out string input))
{
    if (string.IsNullOrEmpty(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;
}

static bool Prompt(out string input)
{
    input = ReadLine.Read("gitlinq> ");
    AnsiConsole.MarkupLine($"[green] typed: [/] {input}");
    return true;
}
