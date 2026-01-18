namespace GitLinq.Models;

/// <summary>
/// Represents a file change in a commit.
/// </summary>
public class FileChange
{
    /// <summary>
    /// The path of the changed file.
    /// </summary>
    public string Path { get; set; } = "";
    
    /// <summary>
    /// The old path (for renamed files).
    /// </summary>
    public string? OldPath { get; set; }
    
    /// <summary>
    /// The type of change: Added, Deleted, Modified, Renamed, Copied.
    /// </summary>
    public string Status { get; set; } = "";
    
    /// <summary>
    /// Number of lines added.
    /// </summary>
    public int LinesAdded { get; set; }
    
    /// <summary>
    /// Number of lines deleted.
    /// </summary>
    public int LinesDeleted { get; set; }
    
    /// <summary>
    /// Whether this is a binary file.
    /// </summary>
    public bool IsBinary { get; set; }
    
    /// <summary>
    /// The actual text content of lines that were added (without the '+' prefix).
    /// </summary>
    public List<string> AddedContent { get; set; } = new();
    
    /// <summary>
    /// The actual text content of lines that were deleted (without the '-' prefix).
    /// </summary>
    public List<string> DeletedContent { get; set; } = new();
    
    /// <summary>
    /// Check if any added line contains the specified text.
    /// </summary>
    public bool AddedContains(string text) => 
        AddedContent.Any(line => line.Contains(text, StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Check if any deleted line contains the specified text.
    /// </summary>
    public bool DeletedContains(string text) => 
        DeletedContent.Any(line => line.Contains(text, StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Check if any changed line (added or deleted) contains the specified text.
    /// </summary>
    public bool ContentContains(string text) => 
        AddedContains(text) || DeletedContains(text);
    
    /// <summary>
    /// Get lines from AddedContent that contain the text, with context (previous and next lines).
    /// </summary>
    public List<MatchedLine> GetAddedMatches(string text)
    {
        var matches = new List<MatchedLine>();
        for (int i = 0; i < AddedContent.Count; i++)
        {
            if (AddedContent[i].Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                var contextLines = new List<(string line, bool isMatch)>();
                
                // Previous line
                if (i > 0)
                    contextLines.Add((AddedContent[i - 1], false));
                
                // Matching line
                contextLines.Add((AddedContent[i], true));
                
                // Next line
                if (i < AddedContent.Count - 1)
                    contextLines.Add((AddedContent[i + 1], false));
                
                matches.Add(new MatchedLine
                {
                    FilePath = Path,
                    MatchType = "added",
                    SearchText = text,
                    ContextLines = contextLines
                });
            }
        }
        return matches;
    }
    
    /// <summary>
    /// Get lines from DeletedContent that contain the text, with context.
    /// </summary>
    public List<MatchedLine> GetDeletedMatches(string text)
    {
        var matches = new List<MatchedLine>();
        for (int i = 0; i < DeletedContent.Count; i++)
        {
            if (DeletedContent[i].Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                var contextLines = new List<(string line, bool isMatch)>();
                
                // Previous line
                if (i > 0)
                    contextLines.Add((DeletedContent[i - 1], false));
                
                // Matching line
                contextLines.Add((DeletedContent[i], true));
                
                // Next line
                if (i < DeletedContent.Count - 1)
                    contextLines.Add((DeletedContent[i + 1], false));
                
                matches.Add(new MatchedLine
                {
                    FilePath = Path,
                    MatchType = "deleted",
                    SearchText = text,
                    ContextLines = contextLines
                });
            }
        }
        return matches;
    }
    
    /// <summary>
    /// Get all content matches (added and deleted) that contain the text, with context.
    /// </summary>
    public List<MatchedLine> GetContentMatches(string text)
    {
        var matches = new List<MatchedLine>();
        matches.AddRange(GetAddedMatches(text));
        matches.AddRange(GetDeletedMatches(text));
        return matches;
    }
}
