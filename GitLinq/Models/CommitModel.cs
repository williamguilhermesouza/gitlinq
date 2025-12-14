namespace GitLinq.Models;

public class CommitModel
{
    public required string Id { get; set; }
    public required string Message { get; set; }
    public required string AuthorName { get; set; }
    public DateTimeOffset When { get; set; }
}