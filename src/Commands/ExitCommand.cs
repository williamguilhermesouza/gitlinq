namespace GitLinq.Commands;

public class ExitCommand : ICommand
{
    public string Name => "exit";
    public string Description => "Exit GitLinq";
    public IReadOnlyList<string> Aliases => ["quit", "q"];
    
    public void Execute(string[] args, CommandContext context)
    {
        context.ShouldExit = true;
    }
}
