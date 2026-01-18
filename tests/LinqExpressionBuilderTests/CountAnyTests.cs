using GitLinq;
using GitLinq.Models;

namespace Tests.LinqExpressionBuilderTests;

[TestClass]
public class CountAnyTests
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
    public void Count_ReturnsTotal()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Count()");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void CountWithPredicate_ReturnsMatchingCount()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Count(c => c.Message.Contains(\"Fix\"))");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void CountWithPredicate_NoMatches_ReturnsZero()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Count(c => c.Message.Contains(\"nonexistent\"))");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void CountAfterWhere_ReturnsFilteredCount()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.AuthorName.Contains(\"Alice\")).Count()");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void Any_WhenCommitsExist_ReturnsTrue()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Any()");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void Any_EmptyList_ReturnsFalse()
    {
        var builder = new LinqExpressionBuilder(new List<CommitInfo>());
        var ast = QueryParser.ParseExpression("Commits.Any()");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void AnyWithPredicate_WhenMatchExists_ReturnsTrue()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Any(c => c.Message.Contains(\"Fix\"))");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void AnyWithPredicate_WhenNoMatch_ReturnsFalse()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Any(c => c.Message.Contains(\"nonexistent\"))");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void AnyAfterWhere_ReturnsCorrectResult()
    {
        var builder = new LinqExpressionBuilder(CreateTestCommits());
        var ast = QueryParser.ParseExpression("Commits.Where(c => c.AuthorName.Contains(\"Charlie\")).Any()");
        
        var result = builder.Execute(ast);
        
        Assert.AreEqual(true, result);
    }
}
