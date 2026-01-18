using Spectre.Console;
using System.Runtime.InteropServices;
using System.Text;

namespace GitLinq.Diagnostics;

/// <summary>
/// Helper class for debug mode diagnostics.
/// </summary>
public static class DebugHelper
{
    /// <summary>
    /// Print environment information for debugging.
    /// </summary>
    public static void PrintEnvironmentInfo()
    {
        var panel = new Panel(
            new Rows(
                new Markup($"[bold]GitLinq Debug Mode[/]"),
                new Text(""),
                new Markup($"[dim]OS:[/] {RuntimeInformation.OSDescription}"),
                new Markup($"[dim]Architecture:[/] {RuntimeInformation.OSArchitecture}"),
                new Markup($"[dim].NET Runtime:[/] {RuntimeInformation.FrameworkDescription}"),
                new Text(""),
                new Markup($"[dim]Console.InputEncoding:[/] {Console.InputEncoding.EncodingName} (CodePage: {Console.InputEncoding.CodePage})"),
                new Markup($"[dim]Console.OutputEncoding:[/] {Console.OutputEncoding.EncodingName} (CodePage: {Console.OutputEncoding.CodePage})"),
                new Text(""),
                new Markup($"[dim]Terminal:[/] {GetTerminalInfo()}"),
                new Markup($"[dim]Working Directory:[/] {Environment.CurrentDirectory}"),
                new Text(""),
                new Markup("[dim]Set GITLINQ_DEBUG=0 or unset to disable debug mode[/]")
            ))
        {
            Header = new PanelHeader("[yellow]Debug Info[/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0)
        };
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Print input information for debugging.
    /// </summary>
    public static void PrintInputInfo(string input, string label)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hexBytes = string.Join(" ", bytes.Select(b => b.ToString("X2")));
        var hasNullBytes = bytes.Contains((byte)0);
        var hasNonAscii = bytes.Any(b => b > 127);
        
        AnsiConsole.MarkupLine($"[dim]Debug - Input ({label}):[/]");
        AnsiConsole.MarkupLine($"[dim]  String: {Markup.Escape(input)}[/]");
        AnsiConsole.MarkupLine($"[dim]  Length: {input.Length} chars, {bytes.Length} bytes[/]");
        AnsiConsole.MarkupLine($"[dim]  Bytes: {hexBytes}[/]");
        
        if (hasNullBytes)
            AnsiConsole.MarkupLine("[yellow]  Warning: Input contains null bytes (0x00)[/]");
        if (hasNonAscii)
            AnsiConsole.MarkupLine("[yellow]  Warning: Input contains non-ASCII characters[/]");
    }

    /// <summary>
    /// Print error information in debug mode.
    /// </summary>
    public static void PrintException(Exception ex)
    {
        AnsiConsole.MarkupLine($"[dim]Debug - Exception type: {ex.GetType().Name}[/]");
        if (ex.InnerException != null)
            AnsiConsole.MarkupLine($"[dim]Debug - Inner exception: {Markup.Escape(ex.InnerException.Message)}[/]");
    }

    /// <summary>
    /// Get information about the current terminal.
    /// </summary>
    private static string GetTerminalInfo()
    {
        var term = Environment.GetEnvironmentVariable("TERM") ?? "not set";
        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? "";
        var wtSession = Environment.GetEnvironmentVariable("WT_SESSION");
        var conEmu = Environment.GetEnvironmentVariable("ConEmuANSI");
        
        if (!string.IsNullOrEmpty(wtSession))
            return "Windows Terminal";
        if (!string.IsNullOrEmpty(conEmu))
            return "ConEmu";
        if (!string.IsNullOrEmpty(termProgram))
            return termProgram;
        if (Environment.GetEnvironmentVariable("VSCODE_INJECTION") != null)
            return "VS Code Integrated Terminal";
            
        // Check for PowerShell or CMD
        var psVersion = Environment.GetEnvironmentVariable("PSVersionTable");
        if (psVersion != null || Environment.GetEnvironmentVariable("PSModulePath") != null)
            return "PowerShell";
        
        return term != "not set" ? term : "Unknown (likely CMD or basic console)";
    }
}
