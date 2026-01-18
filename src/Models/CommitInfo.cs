namespace GitLinq.Models;

/// <summary>
/// Represents a Git commit with its metadata and diff information.
/// </summary>
public class CommitInfo
{
    /// <summary>
    /// The full SHA hash of the commit.
    /// </summary>
    public string Sha { get; set; } = "";
    
    /// <summary>
    /// The full commit message.
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// The first line of the commit message.
    /// </summary>
    public string MessageShort { get; set; } = "";
    
    /// <summary>
    /// The name of the commit author.
    /// </summary>
    public string AuthorName { get; set; } = "";
    
    /// <summary>
    /// The email of the commit author.
    /// </summary>
    public string AuthorEmail { get; set; } = "";
    
    /// <summary>
    /// The timestamp when the commit was authored.
    /// </summary>
    public DateTimeOffset AuthorWhen { get; set; }
    
    /// <summary>
    /// The name of the committer.
    /// </summary>
    public string CommitterName { get; set; } = "";
    
    /// <summary>
    /// The email of the committer.
    /// </summary>
    public string CommitterEmail { get; set; } = "";
    
    /// <summary>
    /// The timestamp when the commit was made.
    /// </summary>
    public DateTimeOffset CommitterWhen { get; set; }
    
    /// <summary>
    /// The diff data for this commit (files changed, lines added/deleted, content).
    /// </summary>
    public DiffData Diff { get; set; } = new();
}
