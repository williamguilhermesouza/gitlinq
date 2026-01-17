namespace GitLinq.Commands;

public class ClearCommand : ICommand
{
    public string Name => "clear";
    public string Description => "Clear the console screen";
    public IReadOnlyList<string> Aliases => ["cls"];
    
    public void Execute(string[] args, CommandContext context)
    {
        Console.Clear();
    }
}