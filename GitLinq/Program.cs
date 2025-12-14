using GitLinq.Models;

namespace GitLinq;

internal static class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine("Mini LINQ-like Git Query demo (type 'exit' to quit)");
            Console.WriteLine("Example queries:");
            Console.WriteLine(
                "  Commits.Where(c => c.AuthorName == \"Alice\" && c.Message.Contains(\"fix\")).FirstOrDefault()");
            Console.WriteLine("  Commits.Where(c => c.Id == \"<sha>\").FirstOrDefault()");
            Console.WriteLine("  Commits.Take(5)");

            Console.WriteLine("Usage: gitlinq [options] query");
            Console.WriteLine("-h, --help               Get this help");
            Console.WriteLine("-q, --query              Apply the linq like query");
            Console.WriteLine("-r, --repository <path>  set the selected repository path as the path to queries");
            return;
        }

        Console.Write("Path to git repository (local folder): ");
        var repoPath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(repoPath) || repoPath == "exit") return;

        while (true)
        {
            Console.Write("gitlinq> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

            try
            {
                var root = QueryParser.ParseQuery(input.Trim());
                Console.WriteLine(
                    $"Parsed root: {root.RootName}, calls: {string.Join(", ", root.Calls.Select(c => c.MethodName))}");
                var result = AstCompiler.ExecuteQuery(root, repoPath);

                switch (result)
                {
                    case CommitModel[] seq:
                        Console.WriteLine("Sequence result:");
                        foreach (var c in seq.Take(50))
                            Console.WriteLine(
                                $"{c.Id[..Math.Min(10, c.Id.Length)]} | {c.AuthorName} | {c.When:yyyy-MM-dd} | {c.Message}");
                        if (seq.Length == 0) Console.WriteLine("(no items)");
                        break;

                    case CommitModel commit:
                        Console.WriteLine(
                                $"{commit.Id} | {commit.AuthorName} | {commit.When:yyyy-MM-dd HH:mm} | {commit.Message}");
                        break;

                    default:
                        Console.WriteLine($"Result: {result}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}