namespace GitLinq.Commands;

public class Clear : ICommand
{
    public string Name => "clear";
    public string Description => "Clear the command line";
    public IReadOnlyList<string> Aliases => ["cls"];
    public void Execute() => Console.Clear();
}