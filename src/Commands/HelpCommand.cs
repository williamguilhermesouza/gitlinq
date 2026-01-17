using Spectre.Console;

namespace GitLinq.Commands;

public class HelpCommand : ICommand
{
    public string Name => "help";
    public string Description => "Show available commands or help for a specific command";
    public IReadOnlyList<string> Aliases => ["?", "commands"];
    public string Usage => "help [command]";
    
    public void Execute(string[] args, CommandContext context)
    {
        if (args.Length > 0)
        {
            ShowCommandHelp(args[0], context);
        }
        else
        {
            ShowAllCommands(context);
        }
    }

    private static void ShowCommandHelp(string commandName, CommandContext context)
    {
        var command = context.Commands.GetCommand(commandName);
        
        if (command == null)
        {
            AnsiConsole.MarkupLine($"[red]Unknown command:[/] {Markup.Escape(commandName)}");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]{command.Name}[/] - {command.Description}");
        AnsiConsole.MarkupLine($"[dim]Usage:[/] {command.Usage}");
        
        if (command.Aliases.Count > 0)
        {
            AnsiConsole.MarkupLine($"[dim]Aliases:[/] {string.Join(", ", command.Aliases)}");
        }
    }

    private static void ShowAllCommands(CommandContext context)
    {
        AnsiConsole.MarkupLine("[bold]Available Commands:[/]\n");
        
        var table = new Table();
        table.Border(TableBorder.None);
        table.HideHeaders();
        table.AddColumn("Command");
        table.AddColumn("Description");

        foreach (var cmd in context.Commands.All.OrderBy(c => c.Name))
        {
            var aliases = cmd.Aliases.Count > 0 
                ? $" [dim]({string.Join(", ", cmd.Aliases)})[/]" 
                : "";
            table.AddRow($"[green]{cmd.Name}[/]{aliases}", cmd.Description);
        }

        AnsiConsole.Write(table);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Type 'help <command>' for more information on a specific command.[/]");
        AnsiConsole.MarkupLine("[dim]Or enter a LINQ query like: Commits.Where(c => c.Message.Contains(\"fix\"))[/]");
    }
}
