namespace GitLinq;

internal class AutoCompletionHandler : IAutoCompleteHandler
{
    // Data sources
    private static readonly string[] DataSources = ["Commits"];

    // Commit properties
    private static readonly string[] CommitProperties =
    [
        "Sha", "Message", "MessageShort", "AuthorName", "AuthorEmail", "AuthorWhen", "Diff"
    ];
    
    // DiffData properties (for c.Diff)
    private static readonly string[] DiffDataProperties =
    [
        "Files", "TotalLinesAdded", "TotalLinesDeleted", "FilesChanged"
    ];
    
    // DiffData methods (for c.Diff.)
    private static readonly Dictionary<string, string> DiffDataMethods = new()
    {
        ["AddedContains"] = "AddedContains('')",
        ["DeletedContains"] = "DeletedContains('')",
        ["ContentContains"] = "ContentContains('')"
    };

    // FileChange properties
    private static readonly string[] FileChangeProperties =
    [
        "Path", "OldPath", "Status", "LinesAdded", "LinesDeleted", "IsBinary",
        "AddedContent", "DeletedContent", "AddedContains", "DeletedContains", "ContentContains"
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
            // After "Commits." suggest LINQ methods
            if (text.StartsWith("Commits", StringComparison.OrdinalIgnoreCase))
            {
                // Check if this is ".Files." context
                if (text.Contains(".Files."))
                    return FileChangeProperties;
                
                // Check if this is ".Diff." context (for Commits)
                if (text.Contains(".Diff."))
                    return [.. DiffDataProperties, .. DiffDataMethods.Keys];
                    
                return LinqMethods.Keys.ToArray();
            }
            
            // After "c." in lambda suggest properties
            if (IsInsideLambda(text))
            {
                // Check for ".Diff." context inside lambda - suggest both properties and methods
                if (text.Contains(".Diff."))
                    return [.. DiffDataProperties, .. DiffDataMethods.Keys];
                // Check for ".Files." context inside lambda (inside Any)
                if (text.Contains(".Files.") || IsInsideFilesAny(text))
                    return FileChangeProperties;
                    
                return CommitProperties;
            }
                
            return CommitProperties;
        }

        // Inside a lambda, after property dot (e.g., "c.Message.")
        if (IsInsideLambda(text) && text.EndsWith('.'))
            return StringMethods.Keys.ToArray();

        // Typing a method name after dot
        if (text.Contains('.') && !text.EndsWith('.'))
        {
            // Check if we're inside a lambda typing
            if (IsInsideLambda(text))
            {
                // Check if we're in Diff context (e.g., "c.Diff.Add")
                if (IsInDiffContext(text))
                {
                    // DiffData methods like AddedContains
                    var diffMethodMatches = DiffDataMethods.Keys
                        .Where(m => m.StartsWith(lastSegment, StringComparison.OrdinalIgnoreCase))
                        .Select(m => DiffDataMethods[m])
                        .ToArray();
                    if (diffMethodMatches.Length > 0)
                        return diffMethodMatches;
                    
                    // DiffData properties
                    var diffPropMatches = DiffDataProperties
                        .Where(p => p.StartsWith(lastSegment, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    if (diffPropMatches.Length > 0)
                        return diffPropMatches;
                }
                
                // String methods like Contains
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
    
    private static bool IsInDiffContext(string text)
    {
        // Check if the last context before current typing is ".Diff."
        var lastDiffIndex = text.LastIndexOf(".Diff.", StringComparison.OrdinalIgnoreCase);
        if (lastDiffIndex < 0) return false;
        
        // Make sure there's no other property access after Diff (like .Files.)
        var afterDiff = text[(lastDiffIndex + 6)..];
        return !afterDiff.Contains('.');
    }
    
    private static bool IsInsideFilesAny(string text)
    {
        // Check if we're inside Files.Any(f => f. context
        return text.Contains(".Files.Any(", StringComparison.OrdinalIgnoreCase) && 
               text.LastIndexOf("=>") > text.LastIndexOf(".Files.Any(", StringComparison.OrdinalIgnoreCase);
    }
}
