using GitLinq;
using GitLinq.Models;

namespace Tests.LinqExpressionBuilderTests;

[TestClass]
public class WhereTests
{
    private static List<CommitInfo> CreateTestCommits()
    {
        return new List<CommitInfo>
        {
            new() { Sha = "abc1234", Message = "Initial commit", MessageShort = "Initial commit", AuthorName = "Alice", AuthorEmail = "alice@test.com", AuthorWhen = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero) },
            new() { Sha = "def5678", Message = "Add feature X", MessageShort = "Add feature X", AuthorName = "Bob", AuthorEmail = "bob@test.com", AuthorWhen = new DateTimeOffset(2025, 1, 2, 11, 0, 0, TimeSpan.Zero) },
            new() { Sha = "ghi9012", Message = "Fix bug in feature X", MessageShort = "Fix bug in feature X", AuthorName = "Alice", AuthorEmail = "alice@test.com", AuthorWhen = new DateTimeOffset(2025, 1, 3, 12, 0, 0, TimeSpan.Zero) },
            new() { Sha = "jkl3456", Message = "Update documentation", MessageShort = "Update documentation", AuthorName = "Charlie", AuthorEmail = "charlie@test.com", AuthorWhen = new DateTimeOffset(2025, 1, 4, 13, 0, 0, TimeSpan.Zero) },
            new() { Sha = "mno7890", Message = "Fix critical bug", MessageShort = "Fix critical bug", AuthorName = "Bob", AuthorEmail = "bob@test.com", AuthorWhen = new DateTimeOffset(2025, 1, 5, 14, 0, 0, TimeSpan.Zero) },
        };
    }

    [TestMethod]
    public void WhereWithContains_FiltersCorrectly()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Message.Contains(\"Fix\"))");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(2, commits.Count);
        Assert.IsTrue(commits.All(c => c.Message.Contains("Fix")));
    }

    [TestMethod]
    public void WhereWithContains_NoMatches_ReturnsEmpty()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Message.Contains(\"nonexistent\"))");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
    }

    [TestMethod]
    public void WhereWithStartsWith_FiltersCorrectly()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Message.StartsWith(\"Fix\"))");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(2, commits.Count);
        Assert.IsTrue(commits.All(c => c.Message.StartsWith("Fix")));
    }

    [TestMethod]
    public void WhereWithEndsWith_FiltersCorrectly()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Message.EndsWith(\"bug\"))");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(1, commits.Count);
        Assert.AreEqual("Fix critical bug", commits[0].Message);
    }

    [TestMethod]
    public void WhereByAuthorName_FiltersCorrectly()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.AuthorName.Contains(\"Alice\"))");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(2, commits.Count);
        Assert.IsTrue(commits.All(c => c.AuthorName == "Alice"));
    }

    [TestMethod]
    public void ChainedWhere_FiltersCorrectly()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Message.Contains(\"Fix\")).Where(c => c.AuthorName.Contains(\"Alice\"))");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(1, commits.Count);
        Assert.AreEqual("ghi9012", commits[0].Sha);
    }
}
