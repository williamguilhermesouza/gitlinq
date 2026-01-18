using GitLinq;
using GitLinq.Models;

namespace Tests.LinqExpressionBuilderTests;

[TestClass]
public class ComplexQueryTests
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
    public void ComplexQuery_WhereSkipTake()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.AuthorName.Contains(\"Alice\")).Skip(1).Take(1)");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(1, commits.Count);
        Assert.AreEqual("ghi9012", commits[0].Sha);
    }

    [TestMethod]
    public void ComplexQuery_MultipleWheres()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Message.Contains(\"Fix\")).Where(c => c.Message.Contains(\"bug\"))");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        var commits = result.ToList();
        Assert.AreEqual(2, commits.Count);
    }

    [TestMethod]
    public void ComplexQuery_WhereThenFirst()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.AuthorName.Contains(\"Bob\")).First()");
        
        var result = builder.Execute(ast) as CommitInfo;
        
        Assert.IsNotNull(result);
        Assert.AreEqual("def5678", result.Sha);
    }

    [TestMethod]
    public void ComplexQuery_WhereThenCount()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Message.StartsWith(\"Fix\")).Count()");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void ComplexQuery_TakeThenFirst()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Take(3).First()");
        
        var result = builder.Execute(ast) as CommitInfo;
        
        Assert.IsNotNull(result);
        Assert.AreEqual("abc1234", result.Sha);
    }

    [TestMethod]
    public void ComplexQuery_SkipThenCount()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Skip(2).Count()");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public void ComplexQuery_WhereThenAny()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.AuthorName.Contains(\"David\")).Any()");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void ComplexQuery_NestedMethodCalls()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.Message.Contains(\"feature\")).Take(5).Skip(1).Count()");
        
        var result = builder.Execute(ast);
        
        // 2 commits contain "feature", skip 1 = 1 remaining
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void QueryAllCommits_ReturnsAll()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits");
        
        var result = builder.Execute(ast) as IEnumerable<CommitInfo>;
        
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Count());
    }
}
