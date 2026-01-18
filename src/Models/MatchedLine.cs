namespace GitLinq.Models;

/// <summary>
/// Represents a matched line with context for display.
/// </summary>
public class MatchedLine
{
    /// <summary>
    /// The file path where the match was found.
    /// </summary>
    public string FilePath { get; set; } = "";
    
    /// <summary>
    /// The type of match: "added" or "deleted".
    /// </summary>
    public string MatchType { get; set; } = "";
    
    /// <summary>
    /// The text that was searched for.
    /// </summary>
    public string SearchText { get; set; } = "";
    
    /// <summary>
    /// The context lines around the match (line content and whether it's the matching line).
    /// </summary>
    public List<(string line, bool isMatch)> ContextLines { get; set; } = new();
}
