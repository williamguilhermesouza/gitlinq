using Spectre.Console;

namespace GitLinq.Commands;

public class HistoryCommand : ICommand
{
    public string Name => "history";
    public string Description => "Show command history";
    public IReadOnlyList<string> Aliases => ["hist"];
    public string Usage => "history [count]";
    
    public void Execute(string[] args, CommandContext context)
    {
        var history = ReadLine.GetHistory();
        
        if (history.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No history yet.[/]");
            return;
        }

        var count = history.Count;
        if (args.Length > 0 && int.TryParse(args[0], out var requestedCount))
        {
            count = Math.Min(requestedCount, history.Count);
        }

        AnsiConsole.MarkupLine($"[bold]Last {count} commands:[/]\n");
        
        var startIndex = Math.Max(0, history.Count - count);
        for (var i = startIndex; i < history.Count; i++)
        {
            AnsiConsole.MarkupLine($"[dim]{i + 1}.[/] {Markup.Escape(history[i])}");
        }
    }
}
