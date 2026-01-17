namespace GitLinq;

internal class AutoCompletionHandler : IAutoCompleteHandler
{
    // Commit properties
    private static readonly string[] CommitProperties =
    [
        "Sha", "Message", "MessageShort", "AuthorName", "AuthorEmail", "AuthorWhen"
    ];

    // LINQ methods with their signatures
    private static readonly Dictionary<string, string> LinqMethods = new()
    {
        ["Where"] = "Where(c => )",
        ["Select"] = "Select(c => )",
        ["Take"] = "Take()",
        ["Skip"] = "Skip()",
        ["First"] = "First()",
        ["FirstOrDefault"] = "FirstOrDefault()",
        ["Count"] = "Count()",
        ["Any"] = "Any()",
        ["OrderBy"] = "OrderBy(c => )",
        ["OrderByDescending"] = "OrderByDescending(c => )"
    };

    // String methods for predicates
    private static readonly Dictionary<string, string> StringMethods = new()
    {
        ["Contains"] = "Contains(\"\")",
        ["StartsWith"] = "StartsWith(\"\")",
        ["EndsWith"] = "EndsWith(\"\")"
    };

    // Commands
    private static readonly string[] Commands = ["help", "examples", "history", "clear", "exit"];

    public char[] Separators { get; set; } = [' ', '.', '('];

    public string[] GetSuggestions(string text, int index)
    {
        if (string.IsNullOrEmpty(text))
            return ["Commits", .. Commands];

        var lastSegment = GetLastSegment(text);
        
        // Starting fresh or typing "Commits"
        if (!text.Contains('.'))
        {
            if ("Commits".StartsWith(lastSegment, StringComparison.OrdinalIgnoreCase))
                return ["Commits"];
            return Commands.Where(c => c.StartsWith(lastSegment, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        // After a dot - suggest methods or properties
        if (text.EndsWith('.'))
        {
            // After "Commits." suggest LINQ methods
            if (text.StartsWith("Commits", StringComparison.OrdinalIgnoreCase))
                return LinqMethods.Keys.ToArray();
            
            // After "c." in lambda suggest properties
            return CommitProperties;
        }

        // Inside a lambda, after property dot (e.g., "c.Message.")
        if (IsInsideLambda(text) && text.EndsWith('.'))
            return StringMethods.Keys.ToArray();

        // Typing a method name after dot
        if (text.Contains('.') && !text.EndsWith('.'))
        {
            // Check if we're inside a lambda typing a string method
            if (IsInsideLambda(text))
            {
                var stringMatches = StringMethods.Keys
                    .Where(m => m.StartsWith(lastSegment, StringComparison.OrdinalIgnoreCase))
                    .Select(m => StringMethods[m])
                    .ToArray();
                if (stringMatches.Length > 0)
                    return stringMatches;
                
                // Typing property name
                var propMatches = CommitProperties
                    .Where(p => p.StartsWith(lastSegment, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (propMatches.Length > 0)
                    return propMatches;
            }

            // Typing LINQ method
            var methodMatches = LinqMethods.Keys
                .Where(m => m.StartsWith(lastSegment, StringComparison.OrdinalIgnoreCase))
                .Select(m => LinqMethods[m])
                .ToArray();
            if (methodMatches.Length > 0)
                return methodMatches;
        }

        return [];
    }

    private static string GetLastSegment(string text)
    {
        var lastDot = text.LastIndexOf('.');
        var lastParen = text.LastIndexOf('(');
        var lastSpace = text.LastIndexOf(' ');
        var lastSeparator = Math.Max(Math.Max(lastDot, lastParen), lastSpace);
        
        return lastSeparator >= 0 ? text[(lastSeparator + 1)..] : text;
    }

    private static bool IsInsideLambda(string text)
    {
        var openParens = text.Count(c => c == '(');
        var closeParens = text.Count(c => c == ')');
        return openParens > closeParens && text.Contains("=>");
    }
}
