namespace GitLinq.Commands;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    IReadOnlyList<string> Aliases { get; }
    void Execute();
}