using GitLinq.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace GitLinq.UI;

/// <summary>
/// Handles displaying query results to the console.
/// </summary>
public static class ResultDisplay
{
    /// <summary>
    /// Display a query result based on its type.
    /// </summary>
    public static void Display(object? result, string? contentSearchText = null)
    {
        switch (result)
        {
            case null:
                AnsiConsole.MarkupLine("[dim](null)[/]");
                break;
            case IEnumerable<CommitInfo> commits:
                DisplayCommitsTable(commits, contentSearchText);
                break;
            case CommitInfo commit:
                DisplayCommitsTable([commit], contentSearchText);
                break;
            case IEnumerable<FileChange> files:
                DisplayFilesTable(files);
                break;
            case FileChange file:
                DisplayFilesTable([file]);
                break;
            case int or long or bool:
                var color = result is bool b ? (b ? "green" : "red") : "green";
                AnsiConsole.MarkupLine($"[{color}]{result}[/]");
                break;
            default:
                AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(result.ToString() ?? "")}[/]");
                break;
        }
    }

    /// <summary>
    /// Display commits in a formatted table.
    /// </summary>
    public static void DisplayCommitsTable(IEnumerable<CommitInfo> commits, string? contentSearchText = null)
    {
        var commitList = commits.ToList();
        
        if (commitList.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No commits found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[green]SHA[/]").Width(10))
            .AddColumn(new TableColumn("[blue]Author[/]").Width(15))
            .AddColumn(new TableColumn("[yellow]Date[/]").Width(17))
            .AddColumn(new TableColumn("[cyan]Files[/]").Width(6).RightAligned())
            .AddColumn(new TableColumn("[green]+[/]").Width(7).RightAligned())
            .AddColumn(new TableColumn("[red]-[/]").Width(7).RightAligned())
            .AddColumn("Message");

        foreach (var commit in commitList)
        {
            var authorDisplay = commit.AuthorName.Length > 15 
                ? commit.AuthorName[..12] + "..." 
                : commit.AuthorName;
            var messageDisplay = commit.MessageShort.Length > 35 
                ? commit.MessageShort[..32] + "..." 
                : commit.MessageShort;
                
            table.AddRow(
                Markup.Escape(commit.Sha[..7]),
                Markup.Escape(authorDisplay),
                commit.AuthorWhen.ToString("yyyy-MM-dd HH:mm"),
                commit.Diff.FilesChanged.ToString(),
                $"[green]+{commit.Diff.TotalLinesAdded}[/]",
                $"[red]-{commit.Diff.TotalLinesDeleted}[/]",
                Markup.Escape(messageDisplay)
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[dim]({commitList.Count} commit{(commitList.Count != 1 ? "s" : "")})[/]");
        
        // If this is a content search, display matching lines with context
        if (!string.IsNullOrEmpty(contentSearchText))
        {
            DisplayContentMatches(commitList, contentSearchText);
        }
    }

    /// <summary>
    /// Display files in a formatted table.
    /// </summary>
    public static void DisplayFilesTable(IEnumerable<FileChange> files)
    {
        var fileList = files.ToList();
        
        if (fileList.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No files found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[yellow]Status[/]").Width(10))
            .AddColumn(new TableColumn("[green]+[/]").Width(6).RightAligned())
            .AddColumn(new TableColumn("[red]-[/]").Width(6).RightAligned())
            .AddColumn("Path");

        foreach (var file in fileList)
        {
            var statusColor = file.Status switch
            {
                "Added" => "green",
                "Deleted" => "red",
                "Modified" => "yellow",
                "Renamed" => "cyan",
                _ => "white"
            };
            
            var path = file.OldPath != null 
                ? $"{file.OldPath} → {file.Path}" 
                : file.Path;

            table.AddRow(
                $"[{statusColor}]{file.Status}[/]",
                $"[green]+{file.LinesAdded}[/]",
                $"[red]-{file.LinesDeleted}[/]",
                Markup.Escape(path)
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[dim]({fileList.Count} file{(fileList.Count != 1 ? "s" : "")})[/]");
    }

    /// <summary>
    /// Display matching content lines with context for content search queries.
    /// </summary>
    private static void DisplayContentMatches(List<CommitInfo> commits, string searchText)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold cyan]Matching lines for '[yellow]{Markup.Escape(searchText)}[/]':[/]");
        AnsiConsole.WriteLine();
        
        var totalMatches = 0;
        
        foreach (var commit in commits)
        {
            var commitMatches = new List<(string filePath, MatchedLine match)>();
            
            foreach (var file in commit.Diff.Files)
            {
                var matches = file.GetContentMatches(searchText);
                foreach (var match in matches)
                {
                    commitMatches.Add((file.Path, match));
                }
            }
            
            if (commitMatches.Count > 0)
            {
                // Create a panel for each commit with matches
                var content = new List<IRenderable>();
                content.Add(new Markup($"[green]{commit.Sha[..7]}[/] [dim]{Markup.Escape(commit.MessageShort)}[/]"));
                content.Add(new Text(""));
                
                foreach (var (filePath, match) in commitMatches)
                {
                    var typeColor = match.MatchType == "added" ? "green" : "red";
                    var typeSymbol = match.MatchType == "added" ? "+" : "-";
                    
                    content.Add(new Markup($"  [dim]{Markup.Escape(filePath)}[/] [bold {typeColor}]({typeSymbol})[/]"));
                    
                    foreach (var (line, isMatch) in match.ContextLines)
                    {
                        var lineContent = line.Length > 80 ? line[..77] + "..." : line;
                        if (isMatch)
                        {
                            // Highlight the matching line and the search text within it
                            var highlighted = HighlightSearchText(lineContent, searchText, typeColor);
                            content.Add(new Markup($"    [bold {typeColor}]→[/] {highlighted}"));
                        }
                        else
                        {
                            content.Add(new Markup($"      [dim]{Markup.Escape(lineContent)}[/]"));
                        }
                    }
                    content.Add(new Text(""));
                    totalMatches++;
                }
                
                var panel = new Panel(new Rows(content))
                {
                    Border = BoxBorder.Rounded,
                    Padding = new Padding(1, 0)
                };
                AnsiConsole.Write(panel);
            }
        }
        
        AnsiConsole.MarkupLine($"[dim]({totalMatches} matching location{(totalMatches != 1 ? "s" : "")})[/]");
    }

    /// <summary>
    /// Highlight search text within a line.
    /// </summary>
    private static string HighlightSearchText(string line, string searchText, string color)
    {
        var index = line.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
        
        if (index >= 0)
        {
            var before = Markup.Escape(line[..index]);
            var match = Markup.Escape(line.Substring(index, searchText.Length));
            var after = Markup.Escape(line[(index + searchText.Length)..]);
            return $"{before}[bold yellow on {color}]{match}[/]{after}";
        }
        
        return Markup.Escape(line);
    }

    /// <summary>
    /// Extract the search text from content search queries like AddedContains, DeletedContains, ContentContains.
    /// Supports both double quotes ("text") and single quotes ('text').
    /// </summary>
    public static string? ExtractContentSearchText(string query)
    {
        // Look for patterns like: AddedContains("text"), DeletedContains("text"), ContentContains("text")
        var patterns = new[] { "AddedContains", "DeletedContains", "ContentContains" };
        
        foreach (var pattern in patterns)
        {
            var index = query.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                // Find the opening parenthesis and extract the string
                var parenStart = query.IndexOf('(', index);
                if (parenStart >= 0)
                {
                    // Try double quotes first
                    var doubleQuoteStart = query.IndexOf('"', parenStart);
                    if (doubleQuoteStart >= 0)
                    {
                        var doubleQuoteEnd = query.IndexOf('"', doubleQuoteStart + 1);
                        if (doubleQuoteEnd > doubleQuoteStart)
                        {
                            return query[(doubleQuoteStart + 1)..doubleQuoteEnd];
                        }
                    }
                    
                    // Try single quotes
                    var singleQuoteStart = query.IndexOf('\'', parenStart);
                    if (singleQuoteStart >= 0)
                    {
                        var singleQuoteEnd = query.IndexOf('\'', singleQuoteStart + 1);
                        if (singleQuoteEnd > singleQuoteStart)
                        {
                            return query[(singleQuoteStart + 1)..singleQuoteEnd];
                        }
                    }
                }
            }
        }
        
        return null;
    }
}
