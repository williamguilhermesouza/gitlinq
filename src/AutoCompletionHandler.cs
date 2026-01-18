namespace GitLinq;

internal class AutoCompletionHandler : IAutoCompleteHandler
{
    // Data sources
    private static readonly string[] DataSources = ["Commits", "Diffs"];

    // Commit properties
    private static readonly string[] CommitProperties =
    [
        "Sha", "Message", "MessageShort", "AuthorName", "AuthorEmail", "AuthorWhen"
    ];

    // CommitDiff properties (for Diffs data source)
    private static readonly string[] DiffProperties =
    [
        "Sha", "ShortSha", "Message", "MessageShort", "AuthorName", "AuthorEmail", "AuthorWhen",
        "Files", "TotalLinesAdded", "TotalLinesDeleted", "FilesChanged"
    ];

    // FileChange properties
    private static readonly string[] FileChangeProperties =
    [
        "Path", "OldPath", "Status", "LinesAdded", "LinesDeleted", "IsBinary"
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
            return [.. DataSources, .. Commands];

        var lastSegment = GetLastSegment(text);
        var isDiffsContext = text.StartsWith("Diffs", StringComparison.OrdinalIgnoreCase);
        
        // Starting fresh or typing data source
        if (!text.Contains('.'))
        {
            var sourceMatches = DataSources
                .Where(s => s.StartsWith(lastSegment, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (sourceMatches.Length > 0)
                return sourceMatches;
            return Commands.Where(c => c.StartsWith(lastSegment, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        // After a dot - suggest methods or properties
        if (text.EndsWith('.'))
        {
            // After "Commits." or "Diffs." suggest LINQ methods
            if (text.StartsWith("Commits", StringComparison.OrdinalIgnoreCase) || 
                text.StartsWith("Diffs", StringComparison.OrdinalIgnoreCase))
            {
                // Check if this is ".Files." context
                if (text.Contains(".Files."))
                    return FileChangeProperties;
                    
                return LinqMethods.Keys.ToArray();
            }
            
            // After "c." or "d." in lambda suggest properties based on context
            if (IsInsideLambda(text))
                return isDiffsContext ? DiffProperties : CommitProperties;
                
            return isDiffsContext ? DiffProperties : CommitProperties;
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
                
                // Typing property name - use context-appropriate properties
                var props = isDiffsContext ? DiffProperties : CommitProperties;
                var propMatches = props
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
