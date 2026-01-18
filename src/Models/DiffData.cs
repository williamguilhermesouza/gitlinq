namespace GitLinq.Models;

/// <summary>
/// Represents the diff data for a commit (files changed, lines added/deleted).
/// </summary>
public class DiffData
{
    /// <summary>
    /// List of file changes in this commit.
    /// </summary>
    public List<FileChange> Files { get; set; } = new();
    
    /// <summary>
    /// Total lines added across all files.
    /// </summary>
    public int TotalLinesAdded => Files.Sum(f => f.LinesAdded);
    
    /// <summary>
    /// Total lines deleted across all files.
    /// </summary>
    public int TotalLinesDeleted => Files.Sum(f => f.LinesDeleted);
    
    /// <summary>
    /// Number of files changed.
    /// </summary>
    public int FilesChanged => Files.Count;
    
    /// <summary>
    /// Check if any file in this diff has added lines containing the specified text.
    /// </summary>
    public bool AddedContains(string text) => 
        Files.Any(f => f.AddedContains(text));
    
    /// <summary>
    /// Check if any file in this diff has deleted lines containing the specified text.
    /// </summary>
    public bool DeletedContains(string text) => 
        Files.Any(f => f.DeletedContains(text));
    
    /// <summary>
    /// Check if any file in this diff has changed lines (added or deleted) containing the specified text.
    /// </summary>
    public bool ContentContains(string text) => 
        Files.Any(f => f.ContentContains(text));
}
