namespace GitLinq.Services;

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
}

/// <summary>
/// Represents a commit with its associated file changes (diff).
/// </summary>
public class CommitDiff
{
    /// <summary>
    /// The commit SHA.
    /// </summary>
    public string Sha { get; set; } = "";
    
    /// <summary>
    /// Short SHA (7 characters).
    /// </summary>
    public string ShortSha => Sha.Length >= 7 ? Sha[..7] : Sha;
    
    /// <summary>
    /// The commit message.
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// Short commit message (first line).
    /// </summary>
    public string MessageShort { get; set; } = "";
    
    /// <summary>
    /// Author name.
    /// </summary>
    public string AuthorName { get; set; } = "";
    
    /// <summary>
    /// Author email.
    /// </summary>
    public string AuthorEmail { get; set; } = "";
    
    /// <summary>
    /// When the commit was authored.
    /// </summary>
    public DateTimeOffset AuthorWhen { get; set; }
    
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
}
