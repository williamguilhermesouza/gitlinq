namespace GitLinq.Commands;

/// <summary>
/// Interface for interactive mode commands.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// The primary name of the command (e.g., "help", "clear").
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// A brief description shown in help.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Alternative names for the command.
    /// </summary>
    IReadOnlyList<string> Aliases { get; }
    
    /// <summary>
    /// Usage syntax shown in help (e.g., "help [command]").
    /// </summary>
    string Usage => Name;
    
    /// <summary>
    /// Execute the command with optional arguments.
    /// </summary>
    /// <param name="args">Arguments passed to the command.</param>
    /// <param name="context">Execution context with access to services.</param>
    void Execute(string[] args, CommandContext context);
}

/// <summary>
/// Context passed to commands during execution.
/// </summary>
public class CommandContext
{
    public required LinqExpressionBuilder ExpressionBuilder { get; init; }
    public required CommandRegistry Commands { get; init; }
    public required Action<object?> DisplayResult { get; init; }
    
    /// <summary>
    /// Signal to exit the interactive loop.
    /// </summary>
    public bool ShouldExit { get; set; }
}