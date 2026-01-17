using System.Reflection;

namespace GitLinq.Commands;

/// <summary>
/// Registry that manages command discovery and lookup.
/// </summary>
public class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ICommand> _allCommands = [];

    /// <summary>
    /// All registered commands.
    /// </summary>
    public IReadOnlyList<ICommand> All => _allCommands;

    /// <summary>
    /// Register a command instance.
    /// </summary>
    public CommandRegistry Register(ICommand command)
    {
        _allCommands.Add(command);
        _commands[command.Name] = command;
        
        foreach (var alias in command.Aliases)
        {
            _commands[alias] = command;
        }
        
        return this;
    }

    /// <summary>
    /// Register multiple commands.
    /// </summary>
    public CommandRegistry Register(params ICommand[] commands)
    {
        foreach (var command in commands)
        {
            Register(command);
        }
        return this;
    }

    /// <summary>
    /// Auto-discover and register all ICommand implementations in the assembly.
    /// </summary>
    public CommandRegistry DiscoverCommands(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        
        var commandTypes = assembly.GetTypes()
            .Where(t => typeof(ICommand).IsAssignableFrom(t) 
                        && t is { IsInterface: false, IsAbstract: false });

        foreach (var type in commandTypes)
        {
            if (Activator.CreateInstance(type) is ICommand command)
            {
                Register(command);
            }
        }

        return this;
    }

    /// <summary>
    /// Try to find a command by name or alias.
    /// </summary>
    public bool TryGetCommand(string nameOrAlias, out ICommand? command)
    {
        return _commands.TryGetValue(nameOrAlias, out command);
    }

    /// <summary>
    /// Get a command by name or alias.
    /// </summary>
    public ICommand? GetCommand(string nameOrAlias)
    {
        _commands.TryGetValue(nameOrAlias, out var command);
        return command;
    }

    /// <summary>
    /// Check if a command exists.
    /// </summary>
    public bool HasCommand(string nameOrAlias)
    {
        return _commands.ContainsKey(nameOrAlias);
    }

    /// <summary>
    /// Try to execute a command from input string.
    /// Returns true if a command was found and executed.
    /// </summary>
    public bool TryExecute(string input, CommandContext context)
    {
        var parts = ParseInput(input);
        if (parts.Length == 0) return false;

        var commandName = parts[0];
        var args = parts.Length > 1 ? parts[1..] : [];

        if (TryGetCommand(commandName, out var command) && command != null)
        {
            command.Execute(args, context);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parse input into command and arguments, respecting quotes.
    /// </summary>
    private static string[] ParseInput(string input)
    {
        var parts = new List<string>();
        var current = "";
        var inQuotes = false;
        var quoteChar = '"';

        foreach (var c in input)
        {
            if ((c == '"' || c == '\'') && !inQuotes)
            {
                inQuotes = true;
                quoteChar = c;
            }
            else if (c == quoteChar && inQuotes)
            {
                inQuotes = false;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (!string.IsNullOrEmpty(current))
                {
                    parts.Add(current);
                    current = "";
                }
            }
            else
            {
                current += c;
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            parts.Add(current);
        }

        return [..parts];
    }
}
